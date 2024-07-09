using Microsoft.EntityFrameworkCore;

namespace WizzServer.Database
{
	public class ApplicationDbContext : DbContext
	{
		public DbSet<DbUser> Users { get; set; }
		public DbSet<Quiz> Quizzes { get; set; }
		public DbSet<History> Histories { get; set; }
		public DbSet<Rating> Ratings { get; set; }
		public static string ConnectionString { get; set; }

		public ApplicationDbContext()
		{
			Database.EnsureCreated();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseNpgsql(ConnectionString);
		}
	}
}
