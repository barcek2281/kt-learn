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
            // Role — нативный enum-тип PostgreSQL user_role, а не строка и не число.
            // Имена значений C# Npgsql переводит в snake_case: Role.Student -> 'student'.
            modelBuilder.HasPostgresEnum<Role>(name: "user_role");
        }
    }
}
