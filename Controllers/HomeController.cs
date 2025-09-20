using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniECommerceStore.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MiniECommerceStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index(string searchString)
        {
            // Include Category and Reviews
            var productsQuery = _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Reviews)
                                        .AsQueryable();

            // Filter products if search string is provided
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery
                    .Where(p => p.Name.Contains(searchString) || p.Description.Contains(searchString));
            }

            var featuredProducts = await productsQuery.Take(4).ToListAsync();
            return View(featuredProducts);
        }
    }
}
