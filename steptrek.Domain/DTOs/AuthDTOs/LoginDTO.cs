using System.ComponentModel.DataAnnotations;

namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(64, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 64 символов")]
        public required string Password { get; set; }
        public string CaptchaToken { get; set; } = "";
    }
}
