using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.File;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.ProjectDTOs;

namespace steptreck.API.Services.Projects
{
    public class ProjectFileServise
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly FileManagerHelper _storage;
        private readonly AuditService _auditService;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public ProjectFileServise(
            AppDbContext context,
            UserHelper userHelper,
            FileManagerHelper storage,
            AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _storage = storage;
            _auditService = auditService;
        }

        public async Task<ProjectFileReadDto> AddProjectFileAsync(
            int projectId,
            IFormFile file,
            CancellationToken ct = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file), "Файл не передан");

            if (file.Length <= 0)
                throw new ArgumentException("Файл пустой или повреждён");

            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException($"Файл слишком большой. Максимум: {MaxFileSizeBytes / (1024 * 1024)} МБ");

            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();

            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.OrganizationId == orgId && !p.IsArchived, ct);

            if (!projectExists)
                throw new KeyNotFoundException("Проект не найден или у вас нет к нему доступа");

            var safeFileName = NormalizeFileName(file.FileName);
            var objectKey = BuildProjectStorageKey(orgId, projectId, safeFileName);

            await using var stream = file.OpenReadStream();
            await _storage.UploadAsync(objectKey, stream, file.ContentType, ct);

            var attachment = new Attachment
            {
                CreatedAt = DateTime.UtcNow,
                OrganizationId = orgId,
                ProjectId = projectId,
                TeamId = null,
                UploadedByUserId = userId,

                FileName = safeFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                StorageKey = objectKey
            };

            _context.Attachments.Add(attachment);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch
            {
                try { await _storage.DeleteAsync(objectKey, ct); } catch {  }
                throw;
            }

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                ProjectId = projectId,
                Action = "create",
                EntityType = "project_file",
                EntityId = attachment.Id,
                EntityName = attachment.FileName,
                Title = "Файл добавлен в проект",
                NewValues = new { attachment.FileName, attachment.ContentType, attachment.SizeBytes }
            }, ct);

            return ToReadDto(attachment);
        }

        public async Task<List<ProjectFileReadDto>> GetProjectFilesAsync(
            int projectId,
            CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var files = await _context.Attachments
                .AsNoTracking()
                .Where(a =>
                    a.OrganizationId == orgId &&
                    a.ProjectId == projectId &&
                    a.Project != null &&
                    !a.Project.IsArchived)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new ProjectFileReadDto
                {
                    Id = a.Id,
                    CreatedAt = a.CreatedAt,
                    OrganizationId = a.OrganizationId,
                    ProjectId = a.ProjectId ?? 0,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes,
                    StorageKey = a.StorageKey
                })
                .ToListAsync(ct);

            return files;
        }

        private static ProjectFileReadDto ToReadDto(Attachment a) => new()
        {
            Id = a.Id,
            CreatedAt = a.CreatedAt,
            OrganizationId = a.OrganizationId,
            ProjectId = a.ProjectId ?? 0,
            FileName = a.FileName,
            ContentType = a.ContentType,
            SizeBytes = a.SizeBytes,
            StorageKey = a.StorageKey
        };

        private static string BuildProjectStorageKey(int orgId, int projectId, string fileName)
        {
            var ext = Path.GetExtension(fileName);
            var guid = Guid.NewGuid().ToString("N");
            var datePart = DateTime.UtcNow.ToString("yyyy-MM");
            return $"org-{orgId}/projects/{projectId}/{datePart}/{guid}{ext}";
        }

        private static string NormalizeFileName(string fileName)
        {
            fileName = (fileName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fileName))
                return "file";

            fileName = Path.GetFileName(fileName);

            const int maxLen = 200;
            if (fileName.Length > maxLen)
            {
                var ext = Path.GetExtension(fileName);
                var name = Path.GetFileNameWithoutExtension(fileName);
                name = name.Substring(0, Math.Min(name.Length, maxLen - ext.Length));
                fileName = name + ext;
            }

            return fileName;
        }
        public async Task<ProjectFileDownloadDto> DownloadProjectFileAsync(
    int attachmentId,
    CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            var attachment = await _context.Attachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == attachmentId &&
                    a.OrganizationId == orgId &&
                    a.ProjectId != null &&
                    a.Project != null &&
                    !a.Project.IsArchived,
                    ct);

            if (attachment is null)
                throw new KeyNotFoundException("Файл не найден или у вас нет к нему доступа");

            if (string.IsNullOrWhiteSpace(attachment.StorageKey))
                throw new InvalidOperationException("У файла отсутствует ключ хранения (StorageKey)");

            var stream = await _storage.DownloadAsync(attachment.StorageKey, ct);

            return new ProjectFileDownloadDto
            {
                FileName = attachment.FileName,
                ContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? "application/octet-stream"
                    : attachment.ContentType,
                Content = stream
            };
        }

        public async Task DeleteProjectFileAsync(int attachmentId, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var attachment = await _context.Attachments
                .FirstOrDefaultAsync(a =>
                    a.Id == attachmentId &&
                    a.OrganizationId == orgId &&
                    a.ProjectId != null &&
                    a.Project != null &&
                    !a.Project.IsArchived,
                    ct);

            if (attachment is null)
                throw new KeyNotFoundException("Файл не найден или у вас нет к нему доступа");

            var storageKey = attachment.StorageKey;

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                ProjectId = attachment.ProjectId,
                Action = "delete",
                EntityType = "project_file",
                EntityId = attachment.Id,
                EntityName = attachment.FileName,
                Title = "Файл удален из проекта",
                OldValues = new { attachment.FileName, attachment.ContentType, attachment.SizeBytes }
            }, ct);

            if (!string.IsNullOrWhiteSpace(storageKey))
            {
                try { await _storage.DeleteAsync(storageKey, ct); } catch { }
            }
        }
    }
}
