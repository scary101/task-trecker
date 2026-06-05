using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 64 символов")]
        [RegularExpression(ValidationPatterns.PasswordStrong,
            ErrorMessage = "Пароль должен содержать заглавные, строчные буквы, цифру и спецсимвол")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 64 символов")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string Confirm { get; set; }

        // ===== ФИО =====

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 50 символов")]
        [RegularExpression(@"^[A-Za-zА-Яа-яЁё\- ]+$",
            ErrorMessage = "Имя может содержать только буквы, пробел и дефис")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилия должна быть от 2 до 50 символов")]
        [RegularExpression(@"^[A-Za-zА-Яа-яЁё\- ]+$",
            ErrorMessage = "Фамилия может содержать только буквы, пробел и дефис")]
        public string Surname { get; set; }

        [StringLength(50, ErrorMessage = "Отчество не должно превышать 50 символов")]
        [RegularExpression(@"^[A-Za-zА-Яа-яЁё\- ]*$",
            ErrorMessage = "Отчество может содержать только буквы, пробел и дефис")]
        public string? Patronymic { get; set; }
        public string CaptchaToken { get; set; } = "";

    }
}
