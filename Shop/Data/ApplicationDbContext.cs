using Microsoft.EntityFrameworkCore;
using Shop.Models;

namespace Shop.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Order { get; set; }

        public DbSet<ProductVote> ProductVotes { get; set; }

    }
}
