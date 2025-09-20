using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniECommerceStore.Models
{
    public class Basket
    {
        public int BasketID { get; set; }
        public int UserID { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public ICollection<BasketItem> Items { get; set; } = new List<BasketItem>();
    }

    public class BasketItem
    {
        public int BasketItemID { get; set; }
        public int BasketID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }

        // Navigation properties
        public Basket? Basket { get; set; }
        public Product? Product { get; set; }
    }



}
