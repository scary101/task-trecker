using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.MemberDTOs;
using steptreck.Domain.Enums;
using System.Text.RegularExpressions;

namespace steptreck.API.Services.Members
{
    public class MemberService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly AuditService _auditService;

        public MemberService(AppDbContext context, UserHelper userHelper, AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _auditService = auditService;
        }

        public async Task<List<MemberDto>> GetAllAsync(CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var rows = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.IsActive)
                .OrderBy(m => m.Surname)
                .Select(m => new
                {
                    m.Id,
                    m.Surname,
                    m.Name,
                    m.Patronymic,
                    RoleName = m.Role.Name,
                    m.IsActive,
                    m.AvatarUrl
                })
                .ToListAsync(ct);

            return rows.Select(m => new MemberDto
            {
                Id = m.Id,
                FullName = string.Join(" ",
                    new[] { m.Surname, m.Name, m.Patronymic }
                        .Where(x => !string.IsNullOrWhiteSpace(x))),
                Role = m.RoleName,
                IsActive = m.IsActive,
                AvatarUtl = m.AvatarUrl
            }).ToList();
        }
        public async Task<string> AddUserNameAsync(string username, CancellationToken ct)
        {
            var userId = _userHelper.GetCurrentUserId();

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == userId, ct);

            if (member == null)
                throw new Exception("Участник не найден");

            username = (username ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(username))
                throw new Exception("Никнейм обязателен");
            if (username.Length < 3 || username.Length > 32)
                throw new Exception("Никнейм должен быть от 3 до 32 символов");
            if (!Regex.IsMatch(username, @"^[a-z0-9_]+$", RegexOptions.CultureInvariant))
                throw new Exception("Никнейм может содержать только латинские буквы, цифры и _");

            var exists = await _context.Members
                .AnyAsync(m => m.Username == username && m.Id != member.Id, ct);

            if (exists)
                throw new Exception("Такое имя уже занято!");

            var oldUsername = member.Username;
            member.Username = username;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(member.OrganizationId, member.Id, new AuditLogCreateDto
            {
                TargetMemberId = member.Id,
                Action = "update",
                EntityType = "member",
                EntityId = member.Id,
                EntityName = _userHelper.GetFullNameMember(member),
                Title = "Обновлен username участника",
                OldValues = new { Username = oldUsername },
                NewValues = new { member.Username }
            }, ct);

            return member.Username;
        }

        public async Task<List<MemberDto>> GetByProject(int projectId, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var rows = await _context.Members
                .AsNoTracking()
                .Where(m =>
                    m.OrganizationId == orgId &&
                    m.IsActive &&
                    m.ProjectTeamMembers.Any(ptm =>
                        ptm.Team.ProjectId == projectId &&
                        ptm.Team.IsActive &&
                        !ptm.Team.Project.IsArchived
                    )
                )
                .OrderBy(m => m.Surname)
                .Select(m => new
                {
                    m.Id,
                    m.Surname,
                    m.Name,
                    m.Patronymic,
                    RoleName = m.Role.Name,
                    m.IsActive,
                    m.AvatarUrl
                })
                .ToListAsync(ct);

            return rows.Select(m => new MemberDto
            {
                Id = m.Id,
                FullName = string.Join(" ",
                    new[] { m.Surname, m.Name, m.Patronymic }
                        .Where(x => !string.IsNullOrWhiteSpace(x))),
                Role = m.RoleName,
                IsActive = m.IsActive,
                AvatarUtl = m.AvatarUrl
            }).ToList();
        }

        public async Task<MemberDto> GetByIdAsync(int memberId, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var member = await _context.Members
                .Include(m => m.Role)
                .FirstOrDefaultAsync(m =>
                    m.Id == memberId &&
                    m.OrganizationId == orgId &&
                    m.IsActive, ct);

            if (member == null)
                throw new InvalidOperationException("Сотрудник не найден");

            return new MemberDto
            {
                Id = member.Id,
                FullName = string.Join(" ",
                    new[] { member.Surname, member.Name, member.Patronymic }
                        .Where(x => !string.IsNullOrWhiteSpace(x))),
                Role = member.Role.Name,
                IsActive = member.IsActive,
                AvatarUtl = member.AvatarUrl
            };
        }

        public async Task<FioInfoDto?> GetFioAsync(CancellationToken ct = default)
        {
            var userId = _userHelper.GetCurrentUserId();

            var member = await _context.Members
                .Where(m => m.UserId == userId && m.IsActive)
                .Select(m => new
                {
                    m.Surname,
                    m.Name,
                    m.Patronymic,
                    m.AvatarUrl
                })
                .FirstOrDefaultAsync(ct);

            if (member is null)
                return null;

            return new FioInfoDto
            {
                AvatarUrl = member.AvatarUrl,
                FullName = string.Join(" ",
                    new[] { member.Surname, member.Name, member.Patronymic }
                        .Where(x => !string.IsNullOrWhiteSpace(x)))
            };
        }



        public async Task UpdateAsync(int memberId, UpdateMemberDto model, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var member = await _context.Members.FirstOrDefaultAsync(m =>
                    m.Id == memberId &&
                    m.OrganizationId == orgId &&
                    m.IsActive, ct);


            if (member == null)
                throw new InvalidOperationException("Сотрудник не найден");

            var actorMemberId = await _userHelper.GetCurrentMemberId(ct);
            var actorRoleId = await _context.Members
                .AsNoTracking()
                .Where(m =>
                    m.Id == actorMemberId &&
                    m.OrganizationId == orgId &&
                    m.IsActive)
                .Select(m => m.RoleId)
                .FirstOrDefaultAsync(ct);

            if (actorRoleId != (int)RoleEnum.Owner && model.RoleId != member.RoleId)
                throw new InvalidOperationException("Недостаточно прав для изменения роли участника");

            var oldValues = new
            {
                member.Surname,
                member.Name,
                member.Patronymic,
                member.RoleId
            };

            member.Surname = model.Surname;
            member.Name = model.Name;
            member.Patronymic = model.Patronymic;
            member.RoleId = model.RoleId;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(orgId, actorMemberId, new AuditLogCreateDto
            {
                TargetMemberId = member.Id,
                Action = "update",
                EntityType = "member",
                EntityId = member.Id,
                EntityName = _userHelper.GetFullNameMember(member),
                Title = "Обновлен участник",
                OldValues = oldValues,
                NewValues = new
                {
                    member.Surname,
                    member.Name,
                    member.Patronymic,
                    member.RoleId
                }
            }, ct);
        }

        public async Task DeleteAsync(int memberId, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var member = await _context.Members
                .FirstOrDefaultAsync(m =>
                    m.Id == memberId &&
                    m.OrganizationId == orgId &&
                    m.IsActive, ct);

            if (member == null)
                throw new InvalidOperationException("Сотрудник не найден");

            var actorMemberId = await _userHelper.GetCurrentMemberId(ct);

            member.IsActive = false;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithAllIdAsync(orgId, actorMemberId, new AuditLogCreateDto
            {
                TargetMemberId = member.Id,
                Action = "delete",
                EntityType = "member",
                EntityId = member.Id,
                EntityName = _userHelper.GetFullNameMember(member),
                Title = "Деактивирован участник",
                OldValues = new { IsActive = true },
                NewValues = new { member.IsActive }
            }, ct);
        }
        public async Task<MemberProfileDto> GetMyProfileAsync(CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            int userId = _userHelper.GetCurrentUserId();

            var dto = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.UserId == userId)
                .Select(m => new MemberProfileDto
                {
                    Id = m.Id,
                    OrganizationName = m.Organization.Name,
                    UserId = m.UserId,

                    Surname = m.Surname,
                    Name = m.Name,
                    Patronymic = m.Patronymic,
                    Username = m.Username,

                    RoleId = m.RoleId,
                    RoleName = m.Role.Name,

                    IsActive = m.IsActive,
                    AvatarUrl = m.AvatarUrl,

                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,

                    Team = m.ProjectTeamMembers
                        .Where(ptm => ptm.Team.IsActive && !ptm.Team.Project.IsArchived)
                        .Select(ptm => new MemberTeamDto
                        {
                            TeamId = ptm.TeamId,
                            TeamName = ptm.Team.Name,
                            TeamDescription = ptm.Team.Description,
                            TeamIsActive = ptm.Team.IsActive,
                            TeamRole = ptm.TeamRole,
                            JoinedAt = ptm.CreatedAt,
                            ProjectId = ptm.Team.ProjectId
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);

            if (dto is null)
                throw new InvalidOperationException("Профиль участника не найден в текущей организации.");

            dto.FullName = BuildFullName(dto.Surname, dto.Name, dto.Patronymic);

            return dto;
        }
        public async Task<MemberProfileDto> GetProfile(int userId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var dto = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.Id == userId)
                .Select(m => new MemberProfileDto
                {
                    Id = m.Id,
                    OrganizationName = m.Organization.Name,
                    UserId = m.UserId,

                    Surname = m.Surname,
                    Name = m.Name,
                    Patronymic = m.Patronymic,
                    Username = m.Username,

                    RoleId = m.RoleId,
                    RoleName = m.Role.Name,

                    IsActive = m.IsActive,
                    AvatarUrl = m.AvatarUrl,

                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,

                    Team = m.ProjectTeamMembers
                        .Where(ptm => ptm.Team.IsActive && !ptm.Team.Project.IsArchived)
                        .Select(ptm => new MemberTeamDto
                        {
                            TeamId = ptm.TeamId,
                            TeamName = ptm.Team.Name,
                            TeamDescription = ptm.Team.Description,
                            TeamIsActive = ptm.Team.IsActive,
                            TeamRole = ptm.TeamRole,
                            JoinedAt = ptm.CreatedAt,
                            ProjectId = ptm.Team.ProjectId
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);
             
            if (dto is null)
                throw new InvalidOperationException("Профиль участника не найден в текущей организации.");

            dto.FullName = BuildFullName(dto.Surname, dto.Name, dto.Patronymic);

            return dto;
        }

        private static string BuildFullName(string surname, string name, string? patronymic)
        {
            if (string.IsNullOrWhiteSpace(patronymic))
                return $"{surname} {name}";
            return $"{surname} {name} {patronymic}";
        }

    }

}
