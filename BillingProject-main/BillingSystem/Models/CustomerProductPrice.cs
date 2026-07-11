using System.ComponentModel.DataAnnotations.Schema;

namespace BillingSystem.Models
{
    public class CustomerProductPrice
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CustomPrice { get; set; }

        public Customer Customer { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}