using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class RegisterOrgDTO
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        [StringLength(254, ErrorMessage = "Email слишком длинный")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 64 символов")]
        [RegularExpression(ValidationPatterns.PasswordStrong,
            ErrorMessage = "Пароль должен содержать заглавные, строчные буквы, цифру и спецсимвол")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 64 символов")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public required string Confirm { get; set; }

        [Required(ErrorMessage = "Название организации обязательно")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 60 символов")]
        [RegularExpression(ValidationPatterns.OrgTeamProjectName, ErrorMessage = "Недопустимые символы в названии")]
        public required string OrgName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилия должна быть от 2 до 50 символов")]
        [RegularExpression(ValidationPatterns.PersonName, ErrorMessage = "Фамилия содержит недопустимые символы")]
        public string Surname { get; set; } = null!;

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 50 символов")]
        [RegularExpression(ValidationPatterns.PersonName, ErrorMessage = "Имя содержит недопустимые символы")]
        public string Name { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Отчество слишком длинное")]
        [RegularExpression(ValidationPatterns.PersonName, ErrorMessage = "Отчество содержит недопустимые символы")]
        public string? Patronymic { get; set; }
        public string CaptchaToken { get; set; } = "";
    }
}
