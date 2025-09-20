using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniECommerceStore.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace MiniECommerceStore.Controllers
{
    [Authorize] // Require authentication for all actions
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method to get current logged-in user's ID
        private int? GetCurrentUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                    return userId;
            }
            return null;
        }

        // GET: Orders (User's own orders)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User) // Include User for username
                .Where(o => o.UserID == userId) // Only this user's orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders); // Index.cshtml
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

            if (order == null)
                return NotFound("Order not found or access denied.");

            return View(order); // Details.cshtml
        }

        // Admin view for all orders
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User) // Include User for username
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders); // AdminIndex.cshtml
        }
    }
}
