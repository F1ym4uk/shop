using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shop.Data;
using Shop.Models;
using System.Text.Json;

[Authorize]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create(string CustomerAddress, string cartData, decimal totalPrice, string? Comment, string? addressCoordinates)
    {
        var userLogin = User.Identity?.Name;
        var user = _context.Users.FirstOrDefault(u => u.Login == userLogin);

        if (user == null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToAction("Index", "Cart");
        }

        var cart = JsonSerializer.Deserialize<List<CartItem>>(cartData);

        if (cart == null || !cart.Any())
        {
            TempData["Error"] = "Корзина пуста.";
            return RedirectToAction("Index", "Cart");
        }

        foreach (var item in cart)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == item.Product.Id);
            if (product != null)
            {
                if (product.StockQuantity >= item.Quantity)
                {
                    product.StockQuantity -= item.Quantity;
                    _context.Update(product);
                }
                else
                {
                    TempData["Error"] = $"Недостаточно товара на складе: {product.Name}";
                    return RedirectToAction("Index", "Cart");
                }
            }
        }

        var productNames = string.Join(", ", cart.Select(c => $"{c.Product.Name} (x{c.Quantity})"));
        var totalQuantity = cart.Sum(c => c.Quantity);

        var fullAddress = !string.IsNullOrEmpty(addressCoordinates)
            ? $"{CustomerAddress} (Координаты: {addressCoordinates})"
            : CustomerAddress;

        var order = new Order
        {
            UserId = user.Id,
            Product = productNames,
            Quantity = totalQuantity,
            Price = totalPrice,
            CustomerAddress = fullAddress,
            Comment = Comment,
            Status = "Новый",
            OrderDate = DateTime.Now
        };

        try
        {
            _context.Order.Add(order);
            _context.SaveChanges();

            HttpContext.Session.Remove("Cart");

            TempData["Success"] = "Заказ успешно оформлен!";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Произошла ошибка при оформлении заказа: {ex.Message}";
            return RedirectToAction("Index", "Cart");
        }
    }

}