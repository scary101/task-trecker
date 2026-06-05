using System.ComponentModel.DataAnnotations;

namespace steptreck.Domain.DTOs.AuthDTOs
{
    public class EmailDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        [StringLength(254, ErrorMessage = "Email слишком длинный")]
        public string Email { get; set; }
    }
}
