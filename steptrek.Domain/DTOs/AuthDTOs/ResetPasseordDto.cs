using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Токен обязателен")]
        [StringLength(256, ErrorMessage = "Токен слишком длинный")]
        [RegularExpression(ValidationPatterns.Token, ErrorMessage = "Токен содержит недопустимые символы")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 64 символов")]
        [RegularExpression(ValidationPatterns.PasswordStrong,
            ErrorMessage = "Пароль должен содержать заглавные, строчные буквы, цифру и спецсимвол")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 64 символов")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string Reset { get; set; }
    }
}
