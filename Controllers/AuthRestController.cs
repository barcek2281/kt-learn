using KT_Learn.Controllers.Dtos;
using KT_Learn.Data;
using KT_Learn.Models;
using KT_Learn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace KT_Learn.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthRestController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly PasswordHasher<User> _passwordHasher = new();
        private readonly TokenService _tokenService;

        public AuthRestController(AppDBContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (emailTaken)
            {
                return BadRequest(new { message = "Email is already taken." });
            }

            var user = new User
            {
                Email = request.Email,
            };
            user.Password = _passwordHasher.HashPassword(user, request.Password);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(
                new
                {
                    message = "user successfully created",
                    user = new
                    {
                        user.Id,
                        user.Email
                    }
                });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var token = _tokenService.CreateToken(user);
            return Ok(new { token });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var email = User.FindFirstValue(JwtRegisteredClaimNames.Email);
            var role = User.FindFirstValue("role");

            return Ok(new {
                id, email, role
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok(new { message = "Ты админ, тебе можно 👑" });
        }
    }
}
