using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data;
using Shop.Models;
using System.Security.Claims;

namespace Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string category, string name, decimal? minPrice, decimal? maxPrice, double? minRating, double? maxRating)
        {
            var productsQuery = _context.Products.Where(p => p.StockQuantity > 0).AsQueryable();

            if (!string.IsNullOrEmpty(name))
                productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(name.ToLower()));

            if (!string.IsNullOrEmpty(category))
                productsQuery = productsQuery.Where(p => p.Category.ToLower().Contains(category.ToLower()));

            if (minPrice.HasValue)
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);

            if (minRating.HasValue)
                productsQuery = productsQuery.Where(p => p.AverageRating >= minRating.Value);

            if (maxRating.HasValue)
                productsQuery = productsQuery.Where(p => p.AverageRating <= maxRating.Value);

            var products = await productsQuery.ToListAsync();

            ViewBag.Names = await productsQuery
                .Select(p => p.Name)
                .Distinct()
                .ToListAsync();

            ViewBag.Categories = await productsQuery
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            var priceRange = await productsQuery
                .GroupBy(p => 1)
                .Select(g => new
                {
                    MinPrice = g.Min(p => p.Price),
                    MaxPrice = g.Max(p => p.Price)
                })
                .FirstOrDefaultAsync();

            ViewBag.MinPriceRange = priceRange?.MinPrice ?? 0;
            ViewBag.MaxPriceRange = priceRange?.MaxPrice ?? 0;

            var ratingRange = await productsQuery
                .GroupBy(p => 1)
                .Select(g => new
                {
                    MinRating = g.Min(p => p.AverageRating),
                    MaxRating = g.Max(p => p.AverageRating)
                })
                .FirstOrDefaultAsync();

            ViewBag.MinRatingRange = ratingRange?.MinRating ?? 0;
            ViewBag.MaxRatingRange = ratingRange?.MaxRating ?? 5;

            List<CartItem> cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartQuantities = cart.ToDictionary(c => c.ProductId, c => c.Quantity);

            ViewBag.CartProductIds = cart.Select(c => c.ProductId).ToList();
            ViewBag.CartQuantities = cartQuantities;

            ViewBag.SelectedName = name;
            ViewBag.SelectedCategory = category;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.MinRating = minRating;
            ViewBag.MaxRating = maxRating;

            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            int? userRating = null;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var vote = await _context.ProductVotes
                    .FirstOrDefaultAsync(v => v.ProductId == id && v.UserId == userId);
                userRating = vote?.Rating;
            }

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                UserRating = userRating
            };

            return View(viewModel);
        }
    }
}