using System.ComponentModel.DataAnnotations;

namespace BillingSystem.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}