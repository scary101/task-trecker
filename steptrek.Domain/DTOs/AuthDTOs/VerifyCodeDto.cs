using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class VerifyCodeDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        [StringLength(254, ErrorMessage = "Email слишком длинный")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Код обязателен")]
        [RegularExpression(ValidationPatterns.Code, ErrorMessage = "Код должен быть от 4 до 8 цифр")]
        public string Code { get; set; } = string.Empty;
    }
}
