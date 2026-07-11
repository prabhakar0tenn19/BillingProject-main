using System.ComponentModel.DataAnnotations.Schema;

namespace BillingSystem.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Rate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        public Invoice Invoice { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}