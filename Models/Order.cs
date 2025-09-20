using Microsoft.EntityFrameworkCore;

namespace MiniECommerceStore.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public Guid PublicOrderID { get; set; } = Guid.NewGuid();

        public string PaymentStatus { get; set; } = "Pending";
        public string? PaymentProvider { get; set; }
        public string? TransactionId { get; set; }

        public string? ShippingAddressLine1 { get; set; }
        public string? ShippingAddressLine2 { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCountry { get; set; }

        public User? User { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }



    public class OrderItem
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }  // FK to Orders
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}