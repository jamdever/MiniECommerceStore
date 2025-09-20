using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniECommerceStore.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Total sales
        var totalSales = await _context.Orders.SumAsync(o => o.Total);

        // Total orders
        var totalOrders = await _context.Orders.CountAsync();

        // Total users
        var totalUsers = await _context.Users.CountAsync();

        // Best-selling products
        var bestSellers = await _context.OrderItems
            .Include(oi => oi.Product)
            .GroupBy(oi => oi.ProductID)
            .Select(g => new
            {
                ProductName = g.First().Product.Name,
                QuantitySold = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalSales = totalSales;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.BestSellers = bestSellers;

        return View();
    }
}
