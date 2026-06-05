using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class UpdateMemberDto
    {
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

        [Range(1, int.MaxValue, ErrorMessage = "Роль обязательна")]
        public int RoleId { get; set; }
    }
}
