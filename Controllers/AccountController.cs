using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniECommerceStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MiniECommerceStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ===========================
        // GET: /Account/SignUp
        // ===========================
        [HttpGet]
        public IActionResult SignUp() => View();

        // ===========================
        // POST: /Account/SignUp
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(string username, string email, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                ViewBag.Error = "Username already exists.";
                return View();
            }

            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Username = username,
                Email = email,
                Password = hasher.HashPassword(null, password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            if (userRole == null)
            {
                userRole = new Role { RoleName = "User" };
                _context.Roles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            _context.UserRoles.Add(new UserRole
            {
                UserID = user.UserID,
                RoleID = userRole.RoleID
            });
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // ===========================
        // GET: /Account/Login
        // ===========================
        [HttpGet]
        public IActionResult Login() => View();

        // ===========================
        // POST: /Account/Login
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter both username and password.";
                return View();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString())
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
            {
                IsPersistent = true
            });

            // Optional: set session basket count
            var userBasketCount = await _context.BasketItems
                .Where(bi => bi.Basket.UserID == user.UserID)
                .SumAsync(bi => bi.Quantity);

            HttpContext.Session.SetInt32("BasketCount", userBasketCount);

            return RedirectToAction("Index", "Home");
        }

        // ===========================
        // Logout
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ===========================
        // GET: /Account/Details
        // ===========================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return NotFound();
            return View(user);
        }

        // ===========================
        // GET: /Account/Edit
        // ===========================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return NotFound();
            return View(user); // sends user including shipping info to the view
        }


        // ===========================
        // POST: /Account/Edit
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(User model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return NotFound();

            user.Username = model.Username;
            user.Email = model.Email;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var hasher = new PasswordHasher<User>();
                user.Password = hasher.HashPassword(user, model.Password);
            }

            // Save shipping info
            user.ShippingAddressLine1 = model.ShippingAddressLine1;
            user.ShippingAddressLine2 = model.ShippingAddressLine2;
            user.ShippingCity = model.ShippingCity;
            user.ShippingState = model.ShippingState;
            user.ShippingPostalCode = model.ShippingPostalCode;
            user.ShippingCountry = model.ShippingCountry;

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account updated successfully!";
            return RedirectToAction("Details");
        }





        // ===========================
        // CREATE ADMIN (one-time)
        // ===========================
        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            var existingAdmin = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == "admin");

            if (existingAdmin != null)
                return Content("Admin already exists.");

            var hasher = new PasswordHasher<User>();
            var admin = new User
            {
                Username = "admin",
                Email = "admin@example.com",
                Password = hasher.HashPassword(null, "Admin@123")
            };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole == null)
            {
                adminRole = new Role { RoleName = "Admin" };
                _context.Roles.Add(adminRole);
                await _context.SaveChangesAsync();
            }

            _context.UserRoles.Add(new UserRole
            {
                UserID = admin.UserID,
                RoleID = adminRole.RoleID
            });
            await _context.SaveChangesAsync();

            return Content("Admin created successfully!");
        }
    }
}
