using System.ComponentModel.DataAnnotations;
using steptreck.Domain.DTOs;

namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class RegisterMebmerDto
    {
        public string CorporateEmail { get; set; } = null!;

        [Range(1, int.MaxValue, ErrorMessage = "Роль обязательна")]
        public int RoleId { get; set; }
    }
}
