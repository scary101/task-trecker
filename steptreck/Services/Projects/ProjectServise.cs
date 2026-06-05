using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using steptreck.API.Gate;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.ProjectDTOs;
using steptreck.Domain.Enums;
using System.Data;

namespace steptreck.API.Services.Projects
{
    public class ProjectServise
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly ISubscriptionGate _gate;
        private readonly AuditService _auditService;

        private static DateTime UtcNowTimestamp() =>
            DateTime.UtcNow;

        public ProjectServise(AppDbContext context, UserHelper userHelper, ISubscriptionGate gate, AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _gate = gate;
            _auditService = auditService;
        }

        public async Task<ProjectReadDto> CreateAsync(ProjectCreateDto dto, CancellationToken ct = default)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto), "Данные проекта не переданы");

            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название проекта обязательно для заполнения");

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            await _gate.ThrowIfCannotCreateProjectAsync(orgId, ct);

            var exists = await _context.Projects
                .AnyAsync(p => p.OrganizationId == orgId && p.Name == name && p.IsArchived != true, ct);

            if (exists)
                throw new InvalidOperationException("Проект с таким названием уже существует в этой организации");

            var project = new Project
            {
                OrganizationId = orgId,
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                GitUrl = string.IsNullOrWhiteSpace(dto.GitUrl) ? null : dto.GitUrl.Trim(),
                IsArchived = false,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                ProjectId = project.Id,
                Action = "create",
                EntityType = "project",
                EntityId = project.Id,
                EntityName = project.Name,
                Title = "Создан проект",
                NewValues = new { project.Name, project.Description, project.GitUrl }
            }, ct);

            return ToReadDto(project);
        }


        public async Task<ProjectReadDto> UpdateAsync(int projectId, ProjectUpdateDto dto, CancellationToken ct = default)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto), "Данные для обновления проекта не переданы");

            var orgId = _userHelper.GetCurrentOrganizationId();

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == orgId, ct);

            if (project is null)
                throw new KeyNotFoundException("Проект не найден или у вас нет к нему доступа");

            var userId = _userHelper.GetCurrentUserId();
            var oldValues = new
            {
                project.Name,
                project.Description,
                project.GitUrl,
                project.IsArchived
            };

            var newName = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Название проекта обязательно для заполнения");

            if (!string.Equals(project.Name, newName, StringComparison.Ordinal))
            {
                var exists = await _context.Projects
                    .AnyAsync(p =>
                        p.OrganizationId == orgId &&
                        p.Name == newName &&
                        p.Id != project.Id,
                        ct);

                if (exists)
                    throw new InvalidOperationException("Проект с таким названием уже существует в этой организации");

                project.Name = newName;
            }

            project.Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim();

            project.GitUrl = string.IsNullOrWhiteSpace(dto.GitUrl)
                ? null
                : dto.GitUrl.Trim();

            if (dto.IsArchived.HasValue)
                project.IsArchived = dto.IsArchived.Value;

            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                ProjectId = project.Id,
                Action = "update",
                EntityType = "project",
                EntityId = project.Id,
                EntityName = project.Name,
                Title = "Обновлен проект",
                OldValues = oldValues,
                NewValues = new
                {
                    project.Name,
                    project.Description,
                    project.GitUrl,
                    project.IsArchived
                }
            }, ct);

            return ToReadDto(project);
        }
        public async Task<List<ProjectReadDto>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var query = _context.Projects
                .AsNoTracking()
                .Where(p => p.OrganizationId == orgId);

            if (!includeArchived)
                query = query.Where(p => !p.IsArchived);

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return projects.Select(ToReadDto).ToList();
        }
        public async Task<ProjectReadDto> GetByIdAsync(int projectId, bool includeArchived = false, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var query = _context.Projects
                .AsNoTracking()
                .Where(p => p.OrganizationId == orgId && p.Id == projectId);

            if (!includeArchived)
                query = query.Where(p => !p.IsArchived);

            var project = await query.FirstOrDefaultAsync(ct);

            if (project is null)
                throw new KeyNotFoundException("Проект не найден или у вас нет к нему доступа");

            return ToReadDto(project);
        }
        public async Task<ProjectReadDto> ToggleArchiveAsync(
            int projectId,
            CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == orgId, ct);

            if (project is null)
                throw new KeyNotFoundException("Проект не найден или у вас нет к нему доступа");

            var userId = _userHelper.GetCurrentUserId();
            var oldIsArchived = project.IsArchived;

            project.IsArchived = !project.IsArchived;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                ProjectId = project.Id,
                Action = "update",
                EntityType = "project",
                EntityId = project.Id,
                EntityName = project.Name,
                Title = project.IsArchived ? "Проект архивирован" : "Проект восстановлен",
                OldValues = new { IsArchived = oldIsArchived },
                NewValues = new { project.IsArchived }
            }, ct);

            return ToReadDto(project);
        }

        public async Task DeleteAsync(int projectId, CancellationToken ct = default)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            var userId = _userHelper.GetCurrentUserId();
            var now = UtcNowTimestamp();

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == orgId, ct);

            if (project is null)
                throw new KeyNotFoundException("Проект не найден или у вас нет к нему доступа");

            project.IsArchived = true;
            project.UpdatedAt = now;

            await _context.ProjectTeams
                .Where(t => t.ProjectId == projectId && t.Project.OrganizationId == orgId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.IsActive, false)
                    .SetProperty(t => t.UpdatedAt, now), ct);

            await _context.ProjectTasks
                .Where(t => t.ProjectId == projectId && t.Project.OrganizationId == orgId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.IsArchived, true)
                    .SetProperty(t => t.UpdatedAt, now), ct);

            await _context.ProjectTeamMembers
                .Where(ptm => ptm.Team.ProjectId == projectId && ptm.Team.Project.OrganizationId == orgId)
                .ExecuteDeleteAsync(ct);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogWithIdAsync(userId, orgId, new AuditLogCreateDto
            {
                ProjectId = project.Id,
                Action = "delete",
                EntityType = "project",
                EntityId = project.Id,
                EntityName = project.Name,
                Title = "Проект удален",
                OldValues = new { IsArchived = false },
                NewValues = new { project.IsArchived }
            }, ct);
        }

        public async Task<List<ProjectLeadDto>> GetProjectLeadsAsync(int projectId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var leads = await _context.ProjectTeamMembers
                .Where(ptm =>
                    ptm.Team.ProjectId == projectId &&
                    ptm.Team.Project.OrganizationId == orgId &&
                    !ptm.Team.Project.IsArchived &&
                    ptm.Team.IsActive &&
                    ptm.Member.OrganizationId == orgId &&
                    ptm.Member.RoleId == (int)RoleEnum.TeamLead
                )
                .Select(ptm => new ProjectLeadDto
                {
                    MemberId = ptm.MemberId,
                    TeamId = ptm.TeamId,
                    TeamName = ptm.Team.Name,
                    RoleInTeam = ptm.TeamRole,

                    FullName =
                        ptm.Member.Surname + " " +
                        ptm.Member.Name +
                        (ptm.Member.Patronymic == null || ptm.Member.Patronymic == ""
                            ? ""
                            : " " + ptm.Member.Patronymic)
                })
                .ToListAsync(ct);

            return leads;
        }

        public async Task<List<ProjectLeadDto>> GetProjectMember(int projectId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var leads = await _context.ProjectTeamMembers
                .Where(ptm =>
                    ptm.Team.ProjectId == projectId &&
                    ptm.Team.Project.OrganizationId == orgId &&
                    !ptm.Team.Project.IsArchived &&
                    ptm.Team.IsActive &&
                    ptm.Member.OrganizationId == orgId &&
                    ptm.Member.RoleId == (int)RoleEnum.Employee
                )
                .Select(ptm => new ProjectLeadDto
                {
                    MemberId = ptm.MemberId,
                    TeamId = ptm.TeamId,
                    TeamName = ptm.Team.Name,
                    RoleInTeam = ptm.TeamRole,

                    FullName =
                        ptm.Member.Surname + " " +
                        ptm.Member.Name +
                        (ptm.Member.Patronymic == null || ptm.Member.Patronymic == ""
                            ? ""
                            : " " + ptm.Member.Patronymic)
                })
                .ToListAsync(ct);

            return leads;
        }



        private static ProjectReadDto ToReadDto(Project project) => new()
        {
            Id = project.Id,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            OrganizationId = project.OrganizationId,
            Name = project.Name,
            Description = project.Description,
            GitUrl = project.GitUrl,
            CardBackgroundUrl = project.CardBackgroundUrl,
            IsArchived = project.IsArchived,
            CreatedByUserId = project.CreatedByUserId
        };
    }
}
