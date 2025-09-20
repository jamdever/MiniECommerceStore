using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniECommerceStore.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public string CategoryName { get; set; } = string.Empty;

        // Navigation property - required by your AppDbContext Product->Category relationship
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
