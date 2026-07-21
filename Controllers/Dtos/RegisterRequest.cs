using System.ComponentModel.DataAnnotations;

namespace KT_Learn.Controllers.Dtos
{
    public class RegisterRequest
    {
        // MaxLength под varchar из схемы: иначе слишком длинное значение
        // упадёт в Postgres пятисоткой вместо понятной 400-й от валидации.
        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
    }
}
