using KT_Learn.Models;
using Microsoft.EntityFrameworkCore;

namespace KT_Learn.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) :base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Enum Role хранится в БД как строка ("Student"/"Admin"), а не как число.
            // Аналог @Enumerated(EnumType.STRING) в JPA.
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();
        }
    }
}
