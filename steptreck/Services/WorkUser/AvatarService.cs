using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;

namespace steptreck.API.Services.WorkUser
{
    public class AvatarService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly IWebHostEnvironment _env;
        private readonly AuditService _auditService;

        private const long MaxSizeBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public AvatarService(
            AppDbContext context,
            UserHelper userHelper,
            IWebHostEnvironment env,
            AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _env = env;
            _auditService = auditService;
        }

        public async Task<string> UploadMyAvatarAsync(IFormFile file, string baseUrl, CancellationToken ct = default)
        {
            var ext = await ValidateFileAsync(file, ct);

            int orgId = _userHelper.GetCurrentOrganizationId();
            int userId = _userHelper.GetCurrentUserId();

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId && m.IsActive, ct);

            if (member == null)
                throw new InvalidOperationException("Сотрудник не найден");

            var result = await SaveFileAsync(
                file,
                ext,
                "avatars",
                $"member{member.Id}",
                member.AvatarUrl,
                baseUrl,
                ct);

            member.AvatarUrl = result.RelativeUrl;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await _auditService.LogWithAllIdAsync(orgId, member.Id, new AuditLogCreateDto
            {
                TargetMemberId = member.Id,
                Action = "update",
                EntityType = "member",
                EntityId = member.Id,
                EntityName = _userHelper.GetFullNameMember(member),
                Title = "Обновлен аватар участника",
                NewValues = new { member.AvatarUrl }
            }, ct);

            return result.FullUrl;
        }


        public async Task<string> UploadProjectBackgroundAsync(
            int projectId,
            IFormFile file,
            string baseUrl,
            CancellationToken ct = default)
        {
            var ext = await ValidateFileAsync(file, ct);
            int orgId = _userHelper.GetCurrentOrganizationId();

            var project = await _context.Projects
                .FirstOrDefaultAsync(p =>
                    p.Id == projectId &&
                    p.OrganizationId == orgId &&
                    !p.IsArchived,
                    ct);

            if (project == null)
                throw new InvalidOperationException("Проект не найден");

            var result = await SaveFileAsync(
                file,
                ext,
                "project-backgrounds",
                $"project{project.Id}",
                project.CardBackgroundUrl,
                baseUrl,
                ct);

            project.CardBackgroundUrl = result.RelativeUrl;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                ProjectId = project.Id,
                Action = "update",
                EntityType = "project",
                EntityId = project.Id,
                EntityName = project.Name,
                Title = "Обновлен фон проекта",
                NewValues = new { project.CardBackgroundUrl }
            }, ct);

            return result.FullUrl;
        }

        public async Task<string> UploadTeamBackgroundAsync(
            int teamId,
            IFormFile file,
            string baseUrl,
            CancellationToken ct = default)
        {
            var ext = await ValidateFileAsync(file, ct);
            int orgId = _userHelper.GetCurrentOrganizationId();

            var team = await _context.ProjectTeams
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t =>
                    t.Id == teamId &&
                    t.IsActive &&
                    !t.Project.IsArchived &&
                    t.Project.OrganizationId == orgId,
                    ct);

            if (team == null)
                throw new InvalidOperationException("Команда не найдена");

            var result = await SaveFileAsync(
                file,
                ext,
                "team-backgrounds",
                $"team{team.Id}",
                team.CardBackgroundUrl,
                baseUrl,
                ct);

            team.CardBackgroundUrl = result.RelativeUrl;
            team.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TeamId = team.Id,
                ProjectId = team.ProjectId,
                Action = "update",
                EntityType = "project_team",
                EntityId = team.Id,
                EntityName = team.Name,
                Title = "Обновлен фон команды",
                NewValues = new { team.CardBackgroundUrl }
            }, ct);

            return result.FullUrl;
        }

        // ===================== HELPERS =====================

        private async Task<string> ValidateFileAsync(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Файл не выбран");

            if (file.Length > MaxSizeBytes)
                throw new InvalidOperationException("Максимум 5MB");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(ext) || !AllowedExt.Contains(ext))
                throw new InvalidOperationException("Разрешены только jpeg/png/webp");

            if (!await IsAllowedImageBySignatureAsync(file, ext, ct))
                throw new InvalidOperationException("Файл не является jpeg/png/webp");

            return ext;
        }

        private async Task<(string RelativeUrl, string FullUrl)> SaveFileAsync(
            IFormFile file,
            string ext,
            string folder,
            string prefix,
            string? oldUrl,
            string baseUrl,
            CancellationToken ct)
        {
            var root = _env.WebRootPath ?? "wwwroot";
            var dir = Path.Combine(root, folder);

            Directory.CreateDirectory(dir);

            var safeExt = ext == ".jpeg" ? ".jpg" : ext;

            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var rnd = Guid.NewGuid().ToString("N")[..8];

            var fileName = $"{prefix}_{stamp}_{rnd}{safeExt}";
            var absPath = Path.Combine(dir, fileName);

            await using (var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs, ct);
            }

            var relativeUrl = $"/{folder}/{fileName}";

            // удаляем старый файл
            if (!string.IsNullOrWhiteSpace(oldUrl) &&
                oldUrl.StartsWith($"/{folder}/", StringComparison.OrdinalIgnoreCase))
            {
                var oldName = oldUrl.Substring($"/{folder}/".Length);
                var oldAbs = Path.Combine(dir, oldName);

                try
                {
                    if (File.Exists(oldAbs))
                        File.Delete(oldAbs);
                }
                catch { }
            }

            return (relativeUrl, $"{baseUrl}{relativeUrl}");
        }

        private static async Task<bool> IsAllowedImageBySignatureAsync(IFormFile file, string ext, CancellationToken ct)
        {
            byte[] header = new byte[12];

            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);

            if (read < 4) return false;

            if (ext == ".png")
                return header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

            if (ext == ".jpg" || ext == ".jpeg")
                return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

            if (ext == ".webp")
            {
                if (read < 12) return false;

                return header[0] == (byte)'R' && header[1] == (byte)'I' &&
                       header[2] == (byte)'F' && header[3] == (byte)'F' &&
                       header[8] == (byte)'W' && header[9] == (byte)'E' &&
                       header[10] == (byte)'B' && header[11] == (byte)'P';
            }

            return false;
        }
    }
}
