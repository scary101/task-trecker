using System.ComponentModel.DataAnnotations;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class UpdateUsernameDto
    {
        [Required(ErrorMessage = "Никнейм обязателен")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Никнейм должен быть от 3 до 32 символов")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Никнейм может содержать только латинские буквы, цифры и _")]
        public string Username { get; set; } = null!;
    }
}
