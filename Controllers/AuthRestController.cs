using KT_Learn.Controllers.Dtos;
using KT_Learn.Models;
using KT_Learn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace KT_Learn.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthRestController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly IAuthService _authService;

        public AuthRestController(TokenService tokenService, IAuthService authService)
        {
            _tokenService = tokenService;
            _authService = authService;
        }

        [HttpPost("register/student")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var user = await _authService.CreateUser(request);

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
            var user = await _authService.LoginUser(request);
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
        [HttpPost("register/admin")]
        public async Task<IActionResult> RegisterAdmin(RegisterRequest request)
        {
            var user = await _authService.CreateAdmin(request);

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
    }
}
