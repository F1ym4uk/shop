using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shop.Models;
using Shop.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;
using Shop.ViewModels;
using ClosedXML.Excel;
using System.Text.RegularExpressions;


namespace Shop.Controllers
{
    public class AccountController : Controller
    {
        
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }


        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(User user)
    {
        if (!ModelState.IsValid)
            return View(user);

        var normalizedLogin = user.Login.Trim().ToLower();
        var normalizedEmail = user.Email.Trim().ToLower();

        bool loginExists = _context.Users
            .Any(u => u.Login.ToLower() == normalizedLogin);

        bool emailExists = _context.Users
            .Any(u => u.Email.ToLower() == normalizedEmail);

        if (loginExists)
            ModelState.AddModelError("Login", "Такой логин уже используется.");

        if (emailExists)
            ModelState.AddModelError("Email", "Такая почта уже используется.");

        if (!ModelState.IsValid)
            return View(user);

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

        _context.Users.Add(user);
        _context.SaveChanges();

        return RedirectToAction("Login", "Account");
    }



    // Get [Edit User]
    [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);

        }

        [HttpPost]  
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User user, string isAdmin)
        {
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            try
            {
                existingUser.Login = user.Login;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;

                if (!string.IsNullOrEmpty(isAdmin))
                {
                    existingUser.IsAdmin = isAdmin == "Да";
                }

                _context.Update(existingUser);
                await _context.SaveChangesAsync();
                return RedirectToAction("AdminUsers");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Ошибка при сохранении изменений. " + ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Произошла непредвиденная ошибка. " + ex.Message);
            }

            return View(user);
        }



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == model.Login);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Неверный логин или пароль.");
                return View(model);
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Login),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(7) // 7 дней входа
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin(string category, string name, decimal? minPrice, decimal? maxPrice)
        {
            var productsQuery = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(name.ToLower()));

            if (!string.IsNullOrEmpty(category))
                productsQuery = productsQuery.Where(p => p.Category.ToLower().Contains(category.ToLower()));

            if (minPrice.HasValue)
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);

            var products = await productsQuery.ToListAsync();

            ViewBag.Categories = await _context.Products.Select(p => p.Category).Distinct().ToListAsync();
            ViewBag.Names = await _context.Products.Select(p => p.Name).Distinct().ToListAsync();
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedName = name;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUsers(string loginInput)
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(loginInput))
            {
                users = users.Where(p => p.Login.Contains(loginInput));
            }

            return View(await users.ToListAsync());
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }

            ViewBag.Message = "Пользователь удалён";
            return RedirectToAction("AdminUsers");
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminOrders()
        {
            var orders = await _context.Order
                .Include(o => o.User)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(orders);
        }



        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Order.Remove(order);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Заказ удален успешно.";
            return RedirectToAction("AdminOrders", "Account");
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Получаем данные пользователя
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Получаем заказы пользователя
            var orders = await _context.Order
                .Where(o => o.UserId == currentUserId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Передаем пользователя в ViewBag
            ViewBag.User = user;

            return View(orders);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (!ModelState.IsValid)
            {
                var orders = await _context.Order
                    .Where(o => o.UserId == currentUserId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.User = await _context.Users.FindAsync(currentUserId);
                return View("MyOrders", orders);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
            {
                TempData["Message"] = "Неверный текущий пароль";
                ModelState.AddModelError("CurrentPassword", "Неверный текущий пароль");
                var orders = await _context.Order
                    .Where(o => o.UserId == currentUserId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.User = user;
                return View("MyOrders", orders);
            }

            var normalizedLogin = model.Login.Trim().ToLower();
            var normalizedEmail = model.Email.Trim().ToLower();

            bool loginExists = await _context.Users
                .AnyAsync(u => u.Id != currentUserId && u.Login.ToLower() == normalizedLogin);
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Id != currentUserId && u.Email.ToLower() == normalizedEmail);

            if (loginExists) { TempData["Message"] = "Введённый логин уже используется"; ModelState.AddModelError("Login", "Такой логин уже используется"); }
            if (emailExists) { TempData["Message"] = "Введённая почта уже используется";  ModelState.AddModelError("Email", "Такая почта уже используется"); }
            if (loginExists && emailExists) { TempData["Message"] = "Введённый логин и почта уже используются";  ModelState.AddModelError("Email", "Такая почта уже используется"); }

            if (!ModelState.IsValid)
            {
                var orders = await _context.Order
                    .Where(o => o.UserId == currentUserId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.User = user;
                return View("MyOrders", orders);
            }

            user.Login = model.Login;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Данные профиля успешно обновлены";
            return RedirectToAction("MyOrders");
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Order.FindAsync(orderId);

            if (order == null || order.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            {
                return NotFound();
            }

            try
            {
                var matches = Regex.Matches(order.Product, @"(.+?) \(x(\d+)\)");

                foreach (Match match in matches)
                {
                    string name = match.Groups[1].Value.Trim();
                    int quantity = int.Parse(match.Groups[2].Value);

                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());

                    if (product != null)
                    {
                        product.StockQuantity += quantity;
                        _context.Update(product);
                    }
                    else
                    {
                        Console.WriteLine($"Товар не найден: '{name}'");
                    }
                }

                order.Status = "Отменён";
                _context.Update(order);

                await _context.SaveChangesAsync();

                TempData["Message"] = "Заказ отменён успешно, товары возвращены на склад!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Ошибка при отмене заказа: " + ex.Message;
            }

            return RedirectToAction("MyOrders");
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> OrderReports(string period = "week")
        {
            DateTime startDate;
            var endDate = DateTime.Now;

            switch (period.ToLower())
            {
                case "month":
                    startDate = endDate.AddMonths(-1);
                    break;
                case "year":
                    startDate = endDate.AddYears(-1);
                    break;
                default: // week
                    startDate = endDate.AddDays(-7);
                    break;
            }

            var orders = await _context.Order
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != "Отменён")
                .Include(o => o.User)
                .ToListAsync();

            var reportData = new OrderReportViewModel
            {
                Orders = orders,
                TotalQuantity = orders.Sum(o => o.Quantity),
                TotalAmount = orders.Sum(o => o.Price),
                StartDate = startDate,
                EndDate = endDate,
                SelectedPeriod = period
            };

            return View(reportData);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportOrderReports(string period = "week")
        {
            DateTime startDate;
            var endDate = DateTime.Now;

            switch (period.ToLower())
            {
                case "month":
                    startDate = endDate.AddMonths(-1);
                    break;
                case "year":
                    startDate = endDate.AddYears(-1);
                    break;
                default: // week
                    startDate = endDate.AddDays(-7);
                    break;
            }

            var orders = await _context.Order
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != "Отменён")
                .Include(o => o.User)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Отчет по заказам");

                // Заголовки
                worksheet.Cell(1, 1).Value = "# заказа";
                worksheet.Cell(1, 2).Value = "Дата заказа";
                worksheet.Cell(1, 3).Value = "Телефон клиента";
                worksheet.Cell(1, 4).Value = "Товары";
                worksheet.Cell(1, 5).Value = "Количество";
                worksheet.Cell(1, 6).Value = "Сумма";
                worksheet.Cell(1, 7).Value = "Адрес доставки";
                worksheet.Cell(1, 8).Value = "Комментарий";
                worksheet.Cell(1, 9).Value = "Статус";

                // Стиль заголовков
                var headerStyle = workbook.Style;
                headerStyle.Font.Bold = true;
                headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerStyle.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Range("A1:I1").Style = headerStyle;

                // Данные
                for (int i = 0; i < orders.Count; i++)
                {
                    var order = orders[i];
                    var row = i + 2;

                    worksheet.Cell(row, 1).Value = order.Id;
                    worksheet.Cell(row, 2).Value = order.OrderDate.ToString("dd.MM.yyyy HH:mm");
                    worksheet.Cell(row, 3).Value = order.User?.PhoneNumber ?? "Не указан";
                    worksheet.Cell(row, 4).Value = order.Product;
                    worksheet.Cell(row, 5).Value = order.Quantity;
                    worksheet.Cell(row, 6).Value = order.Price;
                    worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00\" руб\"";
                    worksheet.Cell(row, 7).Value = order.CustomerAddress;
                    worksheet.Cell(row, 8).Value = order.Comment ?? "-";
                    worksheet.Cell(row, 9).Value = order.Status;
                }

                worksheet.Columns().AdjustToContents();

                // Итоги
                var summaryRow = orders.Count + 3;

                worksheet.Cell(summaryRow, 1).Value = "Период:";
                worksheet.Cell(summaryRow, 2).Value = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                summaryRow++;

                worksheet.Cell(summaryRow, 1).Value = "Всего заказов:";
                worksheet.Cell(summaryRow, 2).Value = orders.Count;
                summaryRow++;

                worksheet.Cell(summaryRow, 1).Value = "Товаров продано:";
                worksheet.Cell(summaryRow, 2).Value = orders.Sum(o => o.Quantity);
                summaryRow++;

                worksheet.Cell(summaryRow, 1).Value = "Общая сумма:";
                worksheet.Cell(summaryRow, 2).Value = orders.Sum(o => o.Price);
                worksheet.Cell(summaryRow, 2).Style.NumberFormat.Format = "#,##0.00\" руб\"";

                worksheet.Range($"A{orders.Count + 3}:B{summaryRow}").Style.Font.Bold = true;

                // Экспорт
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileName = $"Отчет_по_заказам_{period}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var order = await _context.Order.FindAsync(model.Id);
            if (order == null)
                return NotFound();

            // Запрет: нельзя изменить отменённый заказ
            if (order.Status == "Отменён")
                return BadRequest("Нельзя изменить заказ, который уже отменён.");

            // Запрет: нельзя установить статус 'Отменён' через этот метод
            if (model.Status == "Отменён")
                return BadRequest("Отмена заказа должна производиться отдельно.");

            order.Status = model.Status;
            await _context.SaveChangesAsync();

            return Ok();
        }


        public class UpdateOrderStatusModel
        {
            public int Id { get; set; }
            public string Status { get; set; } = string.Empty;
        }




        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile imageFile)
        {
            if (imageFile != null)
            {
                string extension = Path.GetExtension(imageFile.FileName);
                string newFileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(_hostEnvironment.WebRootPath, "images/products", newFileName);

                string productsDir = Path.Combine(_hostEnvironment.WebRootPath, "products");

                Directory.CreateDirectory(productsDir);

                product.Image = newFileName;

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    product.Image = newFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении изображения.");
                    return View(product);
                }
            }

            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Admin));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ошибка при сохранении товара.");
            }

            
            return View(product);
        }

        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.Image))
                {
                    string imagePath = Path.Combine(_hostEnvironment.WebRootPath, "images/Products", product.Image);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Products.Remove(product);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Admin));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(int id, Product product, IFormFile imageFile)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Category = product.Category;

            if (imageFile != null)
            {
                string extension = Path.GetExtension(imageFile.FileName);
                string newFileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(_hostEnvironment.WebRootPath, "images/Products", newFileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(existingProduct.Image))
                    {
                        string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, "images/Products", existingProduct.Image);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    existingProduct.Image = newFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении изображения.");
                    return View(product);
                }
            }

            try
            {
                _context.Products.Update(existingProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction("Admin");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Ошибка при сохранении изменений.");
            }

            return View(product);
        }



        private string ExtractProductName(string productString)
        {
            int index = productString.IndexOf(" (x");
            if (index > 0)
            {
                return productString.Substring(0, index).Trim();
            }
            return productString.Trim();
        }

        private int ExtractProductQuantity(string productString)
        {
            int start = productString.IndexOf("(x") + 2;
            int end = productString.IndexOf(")", start);
            if (start > 1 && end > start)
            {
                string quantityStr = productString.Substring(start, end - start);
                if (int.TryParse(quantityStr, out int quantity))
                {
                    return quantity;
                }
            }
            return 1;
        }


    }


}

