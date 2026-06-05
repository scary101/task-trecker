using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.TaskDTOs.TaskFileDTOs
{
    public class TaskFileUploadDto
    {
        [Required(ErrorMessage = "Файл обязателен")]
        public IFormFile File { get; set; } = null!;
    }
}
