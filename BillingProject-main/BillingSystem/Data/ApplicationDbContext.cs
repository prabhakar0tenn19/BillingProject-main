using Microsoft.EntityFrameworkCore;
using BillingSystem.Models;

namespace BillingSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<CustomerProductPrice> CustomerProductPrices { get; set; }

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerProductPrice>()
                .HasIndex(x => new
                {
                    x.CustomerId,
                    x.ProductId
                })
                .IsUnique();

            modelBuilder.Entity<CustomerProductPrice>()
                .HasOne(x => x.Customer)
                .WithMany(c => c.CustomerPrices)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomerProductPrice>()
                .HasOne(x => x.Product)
                .WithMany(p => p.CustomerPrices)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}