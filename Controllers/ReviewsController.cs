using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniECommerceStore.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MiniECommerceStore.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Ensure user is signed in
        public async Task<IActionResult> Create(int productId, int rating, string comment)
        {
            // Get the logged-in user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                TempData["Error"] = "You must be logged in to submit a review.";
                return RedirectToAction("Login", "Account");
            }

            var review = new Review
            {
                ProductID = productId,
                UserID = userId,
                Rating = rating,
                Comment = comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Products", new { id = productId });
        }




    }
}
