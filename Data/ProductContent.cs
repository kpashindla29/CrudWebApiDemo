using Microsoft.EntityFrameworkCore;

namespace CrudWebApiDemo.Data
{
    public class ProductContext : DbContext
    {
        public ProductContext(DbContextOptions<ProductContext> options) : base(options) { }

        public DbSet<Models.Product> Products { get; set; } // This becomes the Products table
    }
}