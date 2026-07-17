using KT_Learn.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KT_Learn.Data
{
    public static class DbSeeder
    {

        public static async Task SeedSuperAdminAsync(AppDBContext db, IConfiguration configuration)
        {
            var section = configuration.GetSection("SuperAdmin");
            var email = section["Email"];
            var password = section["Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            bool alreadyExists = await db.Users.AnyAsync(u => u.Email == email);
            if (alreadyExists)
            {
                return;
            }

            var admin = new User
            {
                Email = email,
                Role = Role.Admin,
            };
            admin.Password = new PasswordHasher<User>().HashPassword(admin, password);

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
