
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingSystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string ModelName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int UnitsAvailable { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultPrice { get; set; }

        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        public ICollection<CustomerProductPrice> CustomerPrices { get; set; }
            = new List<CustomerProductPrice>();

        public ICollection<InvoiceItem> InvoiceItems { get; set; }
            = new List<InvoiceItem>();
    }
}