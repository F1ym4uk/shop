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
    public IActionResult Create(string CustomerAddress, string cartData, decimal totalPrice)
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

        var productNames = string.Join(", ", cart.Select(c => $"{c.Product.Name} (x{c.Quantity})"));
        var totalQuantity = cart.Sum(c => c.Quantity);

        var order = new Order
        {
            UserId = user.Id,
            Product = productNames,
            Quantity = totalQuantity,
            Price = totalPrice,
            CustomerAddress = CustomerAddress,
            Status = "Новый",
            OrderDate = DateTime.Now
        };

        _context.Order.Add(order);
        _context.SaveChanges();

        HttpContext.Session.Remove("Cart");

        return RedirectToAction("Index", "Home");
    }



}
