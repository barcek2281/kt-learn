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

            // Одного [DatabaseGenerated(Identity)] мало: для Guid EF включает
            // собственный клиентский генератор (с EF 9 он выдаёт UUIDv7) и шлёт
            // значение в INSERT, так что DEFAULT в БД не срабатывает.
            // HasDefaultValueSql помечает колонку как заполняемую БД — EF убирает
            // её из INSERT и забирает результат через RETURNING.
            modelBuilder.Entity<User>()
                .Property(u => u.Id)
                .HasDefaultValueSql("gen_random_uuid()");
        }
    }
}
