using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.InviteDTO
{
    public class AcceptInviteDto
    {
        [Required(ErrorMessage = "Токен обязателен")]
        [StringLength(256, ErrorMessage = "Токен слишком длинный")]
        [RegularExpression(ValidationPatterns.Token, ErrorMessage = "Токен содержит недопустимые символы")]
        public string Token { get; set; } = null!;
    }
}
