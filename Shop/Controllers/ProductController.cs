using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data;
using Shop.Models;

namespace Shop.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProductController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate([FromBody] ProductVote model)
        {
            Console.WriteLine($"Received: ProductId={model.ProductId}, Rating={model.Rating}");

            if (model.Rating < 1 || model.Rating > 5)
                return BadRequest("�������� �������");

            var userId = _userManager.GetUserId(User);
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound("����� �� ������");

            var existingVote = await _context.ProductVotes
                .FirstOrDefaultAsync(v => v.ProductId == model.ProductId && v.UserId == userId);

            if (existingVote != null)
            {
                double oldUserRating = existingVote.Rating;
                existingVote.Rating = model.Rating;

                double oldAverage = product.AverageRating;
                int totalVotes = product.TotalVotes;

                double newAverage = ((oldAverage * totalVotes) - oldUserRating + model.Rating) / totalVotes;

                product.AverageRating = Math.Round(newAverage, 2); 
            }
            else
            {
                var vote = new ProductVote
                {
                    ProductId = model.ProductId,
                    UserId = userId,
                    Rating = model.Rating
                };
                _context.ProductVotes.Add(vote);

                double oldAverage = product.AverageRating;
                int oldTotalVotes = product.TotalVotes;

                double newAverage = ((oldAverage * oldTotalVotes) + model.Rating) / (oldTotalVotes + 1);

                product.AverageRating = Math.Round(newAverage, 2); 
                product.TotalVotes = oldTotalVotes + 1;
            }

            await _context.SaveChangesAsync();

            return Ok(new { averageRating = Math.Round(product.AverageRating, 2), totalVotes = product.TotalVotes });
        }
    }
}