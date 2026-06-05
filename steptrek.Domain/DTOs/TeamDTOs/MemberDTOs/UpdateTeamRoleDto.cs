using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.TeamDTOs.MemberDTOs
{
    public class UpdateTeamRoleDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Участник обязателен")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Роль обязательна")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Роль должна быть от 2 до 50 символов")]
        [RegularExpression(ValidationPatterns.RoleName, ErrorMessage = "Роль содержит недопустимые символы")]
        public string RoleName { get; set; }
    }
}
