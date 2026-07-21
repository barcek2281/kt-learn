using KT_Learn.Controllers.Dtos;
using KT_Learn.Data;
using KT_Learn.Exceptions;
using KT_Learn.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KT_Learn.Services.Impl
{
    public class AuthService : IAuthService
    {
        private readonly AppDBContext _db;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(AppDBContext db, PasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public Task<User> CreateUser(RegisterRequest registerRequest)
        {
            return Create(registerRequest, Role.Student);
        }

        public Task<User> CreateAdmin(RegisterRequest registerRequest)
        {
            return Create(registerRequest, Role.Admin);
        }

        public async Task<User> LoginUser(LoginRequest loginRequest)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
            if (user is null)
            {
                // Не уточняем, что именно неверно — иначе можно перебором узнать, какие email зарегистрированы.
                throw new UnauthorizedException("Invalid email or password.");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, loginRequest.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.Password = _passwordHasher.HashPassword(user, loginRequest.Password);
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return user;
        }

        private async Task<User> Create(RegisterRequest registerRequest, Role role)
        {
            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == registerRequest.Email);
            if (emailTaken)
            {
                throw new ConflictException($"Пользователь с email {registerRequest.Email} уже существует.");
            }

            var user = new User
            {
                Email = registerRequest.Email,
                Role = role
            };
            user.Password = _passwordHasher.HashPassword(user, registerRequest.Password);

            _db.Users.Add(user);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
            {
                // Проверка выше могла не сработать: между ней и вставкой параллельный
                // запрос успел занять этот email. Ловит UNIQUE-индекс в БД.
                throw new ConflictException($"Пользователь с email {registerRequest.Email} уже существует.");
            }

            return user;
        }
    }
}
