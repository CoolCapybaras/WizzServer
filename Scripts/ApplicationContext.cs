using Microsoft.EntityFrameworkCore;

namespace WizzServer
{
	public class ApplicationContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; }

        public ApplicationContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=пароль_от_postgres");
        }
    }
}
