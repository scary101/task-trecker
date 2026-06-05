using System.ComponentModel.DataAnnotations;

namespace steptreck.Domain.DTOs.TaskDTOs.CheckListDTOs
{
    public class SaveChecklistDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Задача обязательна")]
        public int TaskId { get; set; }

        [MinLength(1, ErrorMessage = "Нужно добавить минимум 1 пункт")]
        public List<ChecklistUpsertDto> Items { get; set; } = new();
    }
}
