using KT_Learn.Controllers.Dtos;
using KT_Learn.Models;

namespace KT_Learn.Services
{
    public interface IAuthService
    {
        Task<User> CreateUser(RegisterRequest registerRequest);
        Task<User> LoginUser(LoginRequest loginRequest);
        Task<User> CreateAdmin(RegisterRequest registerRequest);
    }
}
