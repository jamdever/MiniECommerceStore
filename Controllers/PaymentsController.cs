using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MiniECommerceStore.Models;
using Stripe;
using Stripe.Checkout;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly StripeSettings _stripeSettings;

    public PaymentsController(AppDbContext context, IOptions<StripeSettings> stripeOptions)
    {
        _context = context;
        _stripeSettings = stripeOptions.Value;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"];
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _stripeSettings.WebhookSecret
            );
        }
        catch (StripeException e)
        {
            return BadRequest(new { error = e.Message });
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                // Handle successful checkout
                var session = stripeEvent.Data.Object as Session;
                if (session != null &&
                    session.Metadata != null &&
                    session.Metadata.TryGetValue("orderId", out var orderIdStr) &&
                    int.TryParse(orderIdStr, out var orderId))
                {
                    var order = _context.Orders
                        .Include(o => o.Items)
                        .FirstOrDefault(o => o.OrderID == orderId);

                    if (order != null)
                    {
                        // Mark order as paid
                        order.PaymentStatus = "Paid";
                        order.TransactionId = session.PaymentIntentId;
                        _context.Orders.Update(order);

                        // Deduct product stock
                        foreach (var item in order.Items)
                        {
                            var product = _context.Products.FirstOrDefault(p => p.ProductID == item.ProductID);
                            if (product != null)
                            {
                                product.Stock -= item.Quantity;
                                if (product.Stock < 0) product.Stock = 0;
                                _context.Products.Update(product);
                            }
                        }

                        // Remove basket items **only on successful payment**
                        var basket = _context.Baskets
                            .Include(b => b.Items)
                            .FirstOrDefault(b => b.UserID == order.UserID);

                        if (basket != null)
                        {
                            _context.BasketItems.RemoveRange(basket.Items);
                            _context.Baskets.Remove(basket);
                        }

                        await _context.SaveChangesAsync();
                    }
                }
                break;

            case "payment_intent.payment_failed":
                // Optional: handle failed payments
                var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                // Log or update DB as needed
                break;
        }

        return Ok();
    }
}
