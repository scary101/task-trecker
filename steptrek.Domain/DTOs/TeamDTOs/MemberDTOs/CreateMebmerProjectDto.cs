using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.TeamDTOs.MemberDTOs
{
    public class CreateMebmerProjectDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Команда обязательна")]
        public int TeamId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Участник обязателен")]
        public int MemberId { get; set; }

        [StringLength(50, ErrorMessage = "Роль слишком длинная")]
        [RegularExpression(ValidationPatterns.RoleName, ErrorMessage = "Роль содержит недопустимые символы")]
        public string? TeamRole { get; set; }
    }

    public class CreateLeadMebmerProjectDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Команда обязательна")]
        public int TeamId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Участник обязателен")]
        public int MemberId { get; set; }
    }
}
