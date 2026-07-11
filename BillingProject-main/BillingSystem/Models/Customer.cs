using System.ComponentModel.DataAnnotations;

namespace BillingSystem.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        public string? GstNumber { get; set; }

        public string? ContactPerson { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Invoice> Invoices { get; set; }
            = new List<Invoice>();

        public ICollection<CustomerProductPrice> CustomerPrices { get; set; }
            = new List<CustomerProductPrice>();
    }
}