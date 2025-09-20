namespace MiniECommerceStore.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryID { get; set; }
        public string? ImageFileName { get; set; }

        // Navigation properties
        public Category? Category { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
