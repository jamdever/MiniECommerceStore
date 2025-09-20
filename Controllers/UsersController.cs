using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniECommerceStore.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();
        return View(users);
    }

    // POST: Deactivate user
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        _context.Update(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"User {user.Username} has been deactivated.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Activate user
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = true;
        _context.Update(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"User {user.Username} has been activated.";
        return RedirectToAction(nameof(Index));
    }
}

