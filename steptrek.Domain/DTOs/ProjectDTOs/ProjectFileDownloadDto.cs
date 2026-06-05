using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ProjectDTOs
{
    public class ProjectFileDownloadDto
    {
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public Stream Content { get; set; } = null!;
    }
}
