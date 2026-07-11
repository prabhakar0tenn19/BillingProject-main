using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingSystem.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public int CustomerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GstAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        public string? SignatureFile { get; set; }

        public string? Remarks { get; set; }

        public Customer Customer { get; set; } = null!;

        public ICollection<InvoiceItem> InvoiceItems { get; set; }
            = new List<InvoiceItem>();
    }
}