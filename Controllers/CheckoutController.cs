using Microsoft.AspNetCore.Mvc;
using MiniECommerceStore.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiniECommerceStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;
        private readonly StripeSettings _stripeSettings;
        private const string SessionKey = "Basket";

        public CheckoutController(AppDbContext context, IOptions<StripeSettings> stripeOptions)
        {
            _context = context;
            _stripeSettings = stripeOptions.Value;
        }

        private int? GetCurrentUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId)) return userId;
            }
            return null;
        }

        private List<CartItem> GetBasket()
        {
            var userId = GetCurrentUserId();
            if (userId != null)
            {
                var basketItems = _context.BasketItems
                    .Include(bi => bi.Product)
                    .Include(bi => bi.Basket)
                    .Where(bi => bi.Basket.UserID == userId)
                    .ToList();

                return basketItems.Select(bi => new CartItem
                {
                    ProductID = bi.ProductID,
                    Name = bi.Product.Name,
                    Price = bi.Product.Price,
                    Quantity = bi.Quantity
                }).ToList();
            }
            else
            {
                var basketJson = HttpContext.Session.GetString(SessionKey);
                return string.IsNullOrEmpty(basketJson)
                    ? new List<CartItem>()
                    : JsonConvert.DeserializeObject<List<CartItem>>(basketJson);
            }
        }

        [Authorize]
        public IActionResult Index()
        {
            var basket = GetBasket();
            if (!basket.Any())
            {
                TempData["Error"] = "Your basket is empty!";
                return RedirectToAction("Index", "Basket");
            }

            var userId = GetCurrentUserId();
            var user = _context.Users.FirstOrDefault(u => u.UserID == userId);

            ViewBag.GrandTotal = basket.Sum(i => i.Price * i.Quantity);
            ViewBag.User = user;

            // Pre-fill shipping info
            ViewBag.ShippingAddressLine1 = user?.ShippingAddressLine1 ?? "";
            ViewBag.ShippingAddressLine2 = user?.ShippingAddressLine2 ?? "";
            ViewBag.ShippingCity = user?.ShippingCity ?? "";
            ViewBag.ShippingState = user?.ShippingState ?? "";
            ViewBag.ShippingPostalCode = user?.ShippingPostalCode ?? "";
            ViewBag.ShippingCountry = user?.ShippingCountry ?? "";

            return View(basket);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> PlaceOrder(
    string shippingAddressLine1,
    string shippingAddressLine2,
    string shippingCity,
    string shippingState,
    string shippingPostalCode,
    string shippingCountry)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var basket = GetBasket();
            if (!basket.Any())
            {
                TempData["Error"] = "Your basket is empty!";
                return RedirectToAction("Index", "Basket");
            }

            // 1️⃣ Save user's shipping info
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user != null)
            {
                user.ShippingAddressLine1 = shippingAddressLine1;
                user.ShippingAddressLine2 = shippingAddressLine2;
                user.ShippingCity = shippingCity;
                user.ShippingState = shippingState;
                user.ShippingPostalCode = shippingPostalCode;
                user.ShippingCountry = shippingCountry;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            // 2️⃣ Create order in database (status: Pending)
            var order = new Order
            {
                UserID = userId.Value,
                Total = basket.Sum(i => i.Price * i.Quantity),
                OrderDate = DateTime.Now,
                PublicOrderID = Guid.NewGuid(),
                PaymentStatus = "Pending",
                PaymentProvider = "Stripe",
                ShippingAddressLine1 = shippingAddressLine1,
                ShippingAddressLine2 = shippingAddressLine2,
                ShippingCity = shippingCity,
                ShippingState = shippingState,
                ShippingPostalCode = shippingPostalCode,
                ShippingCountry = shippingCountry,
                Items = basket.Select(i => new OrderItem
                {
                    ProductID = i.ProductID,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 3️⃣ Create Stripe Checkout session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = basket.Select(i => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(i.Price * 100), // amount in cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = i.Name
                        }
                    },
                    Quantity = i.Quantity
                }).ToList(),
                Mode = "payment",
                SuccessUrl = Url.Action("PaymentSuccess", "Checkout", new { orderId = order.OrderID }, Request.Scheme),
                CancelUrl = Url.Action("Index", "Checkout", null, Request.Scheme)
            };

            var service = new SessionService();
            Session session = service.Create(options);

            // 4️⃣ Redirect user to Stripe Checkout page
            return Redirect(session.Url);
        }


        [Authorize]
        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            var order = await _context.Orders.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order != null)
            {
                order.PaymentStatus = "Paid";

                // Deduct stock
                foreach (var item in order.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductID);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                        if (product.Stock < 0) product.Stock = 0;
                    }
                }

                // Remove basket
                var basketEntity = await _context.Baskets.Include(b => b.Items)
                    .FirstOrDefaultAsync(b => b.UserID == order.UserID);

                if (basketEntity != null)
                {
                    _context.BasketItems.RemoveRange(basketEntity.Items);
                    _context.Baskets.Remove(basketEntity);
                }

                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("Basket", "");
                HttpContext.Session.SetInt32("BasketCount", 0);

                TempData["Success"] = "Your payment was successful! Order placed.";
            }

            return RedirectToAction("Index", "Orders");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateShippingInfo(User model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return NotFound();

            user.ShippingAddressLine1 = model.ShippingAddressLine1;
            user.ShippingAddressLine2 = model.ShippingAddressLine2;
            user.ShippingCity = model.ShippingCity;
            user.ShippingState = model.ShippingState;
            user.ShippingPostalCode = model.ShippingPostalCode;
            user.ShippingCountry = model.ShippingCountry;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Shipping info updated successfully!";
            return RedirectToAction("Index");
        }


    }
}
