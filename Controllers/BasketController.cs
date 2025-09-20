using Microsoft.AspNetCore.Mvc;
using MiniECommerceStore.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


namespace MiniECommerceStore.Controllers
{
    public class BasketController : Controller
    {
        private readonly AppDbContext _context;
        private const string SessionKey = "Basket";

        public BasketController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method to get current user ID from claims
        private int? GetCurrentUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    return userId;
                }
            }
            return null;
        }

        // Get basket for current user or guest session
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

        // Save basket to DB for logged-in users or session for guests
        private void SaveBasket(List<CartItem> basket)
        {
            var userId = GetCurrentUserId();

            if (userId != null)
            {
                // Ensure the user has a basket in DB
                var basketEntity = _context.Baskets.FirstOrDefault(b => b.UserID == userId);
                if (basketEntity == null)
                {
                    basketEntity = new Basket { UserID = userId.Value };
                    _context.Baskets.Add(basketEntity);
                    _context.SaveChanges();
                }

                foreach (var item in basket)
                {
                    var dbItem = _context.BasketItems
                        .FirstOrDefault(bi => bi.BasketID == basketEntity.BasketID && bi.ProductID == item.ProductID);

                    if (dbItem != null)
                        dbItem.Quantity = item.Quantity;
                    else
                        _context.BasketItems.Add(new BasketItem
                        {
                            BasketID = basketEntity.BasketID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity
                        });
                }

                _context.SaveChanges();
            }
            else
            {
                var basketJson = JsonConvert.SerializeObject(basket);
                HttpContext.Session.SetString(SessionKey, basketJson);
            }

            HttpContext.Session.SetInt32("BasketCount", basket.Sum(i => i.Quantity));
        }

        // Display basket
        public IActionResult Index()
        {
            var basket = GetBasket();
            if (!basket.Any())
            {
                TempData["Error"] = "Your basket is empty!";
                return View(new List<CartItem>());
            }

            ViewBag.GrandTotal = basket.Sum(i => i.Price * i.Quantity);
            return View(basket);
        }

        // Add product to basket
        public IActionResult Add(int productId, string name, decimal price)
        {
            var basket = GetBasket();
            var item = basket.FirstOrDefault(x => x.ProductID == productId);

            if (item == null)
            {
                basket.Add(new CartItem
                {
                    ProductID = productId,
                    Name = name,
                    Price = price,
                    Quantity = 1
                });
            }
            else
            {
                item.Quantity++;
            }

            SaveBasket(basket);
            return RedirectToAction("Index");
        }
        // Remove product from basket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var userId = GetCurrentUserId();
            var basket = GetBasket();

            var item = basket.FirstOrDefault(i => i.ProductID == productId);
            if (item != null)
            {
                // Decrease quantity by 1
                item.Quantity--;

                if (item.Quantity <= 0)
                {
                    basket.Remove(item);
                }

                if (userId != null)
                {
                    // Logged-in user: update DB
                    var basketEntity = _context.Baskets
                        .Include(b => b.Items)
                        .FirstOrDefault(b => b.UserID == userId);

                    if (basketEntity != null)
                    {
                        var dbItem = basketEntity.Items.FirstOrDefault(bi => bi.ProductID == productId);
                        if (dbItem != null)
                        {
                            if (item.Quantity > 0)
                            {
                                dbItem.Quantity = item.Quantity;
                                _context.BasketItems.Update(dbItem);
                            }
                            else
                            {
                                _context.BasketItems.Remove(dbItem);
                            }
                            _context.SaveChanges();
                        }
                    }
                }
                else
                {
                    // Guest: update session
                    var basketJson = JsonConvert.SerializeObject(basket);
                    HttpContext.Session.SetString(SessionKey, basketJson);
                }

                // Update basket count
                HttpContext.Session.SetInt32("BasketCount", basket.Sum(i => i.Quantity));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, string name, decimal price, int quantity = 1)
        {
            var basket = GetBasket();
            var item = basket.FirstOrDefault(x => x.ProductID == productId);

            if (item == null)
            {
                basket.Add(new CartItem
                {
                    ProductID = productId,
                    Name = name,
                    Price = price,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
            }

            SaveBasket(basket);
            return RedirectToAction("Index", "Basket");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Increment(int productId)
        {
            var basket = GetBasket();
            var item = basket.FirstOrDefault(i => i.ProductID == productId);

            if (item != null)
            {
                item.Quantity++;

                var userId = GetCurrentUserId();
                if (userId != null)
                {
                    var basketEntity = _context.Baskets
                        .Include(b => b.Items)
                        .FirstOrDefault(b => b.UserID == userId);

                    if (basketEntity != null)
                    {
                        var dbItem = basketEntity.Items.FirstOrDefault(bi => bi.ProductID == productId);
                        if (dbItem != null)
                        {
                            dbItem.Quantity = item.Quantity;
                            _context.BasketItems.Update(dbItem);
                            _context.SaveChanges();
                        }
                    }
                }
                else
                {
                    // Guest: update session
                    var basketJson = JsonConvert.SerializeObject(basket);
                    HttpContext.Session.SetString(SessionKey, basketJson);
                }

                HttpContext.Session.SetInt32("BasketCount", basket.Sum(i => i.Quantity));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrement(int productId)
        {
            var basket = GetBasket();
            var item = basket.FirstOrDefault(i => i.ProductID == productId);

            if (item != null)
            {
                item.Quantity--;

                if (item.Quantity <= 0)
                    basket.Remove(item);

                var userId = GetCurrentUserId();
                if (userId != null)
                {
                    var basketEntity = _context.Baskets
                        .Include(b => b.Items)
                        .FirstOrDefault(b => b.UserID == userId);

                    if (basketEntity != null)
                    {
                        var dbItem = basketEntity.Items.FirstOrDefault(bi => bi.ProductID == productId);
                        if (dbItem != null)
                        {
                            if (item.Quantity > 0)
                                dbItem.Quantity = item.Quantity;
                            else
                                _context.BasketItems.Remove(dbItem);

                            _context.SaveChanges();
                        }
                    }
                }
                else
                {
                    // Guest: update session
                    var basketJson = JsonConvert.SerializeObject(basket);
                    HttpContext.Session.SetString(SessionKey, basketJson);
                }

                HttpContext.Session.SetInt32("BasketCount", basket.Sum(i => i.Quantity));
            }

            return RedirectToAction("Index");
        }




    }
}
