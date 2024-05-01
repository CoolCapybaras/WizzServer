using Microsoft.EntityFrameworkCore;

namespace WizzServer.Database
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<DbUser> Users { get; set; }

        public ApplicationDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=wizz;Username=postgres;Password=11022004");
        }
    }
}
