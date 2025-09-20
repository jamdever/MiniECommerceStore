using System;
using System.ComponentModel.DataAnnotations;

namespace MiniECommerceStore.Models
{
    public class Review
    {
        public int ReviewID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public int UserID { get; set; }  // Link to user

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [Required, StringLength(1000)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Product Product { get; set; }
        public User User { get; set; }
    }

}
