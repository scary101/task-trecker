using Microsoft.EntityFrameworkCore;
using steptreck.API.Gate;
using steptreck.API.Infrastructure.Email;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.MemberDTOs;
using steptreck.Domain.DTOs.TeamDTOs;
using steptreck.Domain.DTOs.TeamDTOs.MemberDTOs;
using steptreck.Domain.Enums;
using System.Data;

namespace steptreck.API.Services.Projects
{
    public class ProjectTeamService
    {
        private const string TeamLeadRoleTitle = "Руководитель команды";
        public const string DuplicateTeamNameMessage = "Команда с таким названием уже существует в этом проекте.";

        private readonly AppDbContext _context;
        private readonly EmailHelper _emailHelper;
        private readonly UserHelper _userHelper;
        private readonly ISubscriptionGate _gate;
        private readonly AuditService _auditService;

        private static DateTime UtcNowTimestamp() =>
            DateTime.UtcNow;

        public ProjectTeamService(
            AppDbContext context,
            EmailHelper emailHelper,
            UserHelper userHelper,
            ISubscriptionGate gate,
            AuditService auditService)
        {
            _context = context;
            _emailHelper = emailHelper;
            _userHelper = userHelper;
            _gate = gate;
            _auditService = auditService;
        }

        public async Task<ProjectTeam> CreateTeam(CreateTeamDto model, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            var name = (model.Name ?? string.Empty).Trim();
            var normalizedName = name.ToLower();

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            await _gate.ThrowIfCannotCreateTeamAsync(orgId, ct);

            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.Id == model.ProjectId && p.OrganizationId == orgId && !p.IsArchived, ct);

            if (!projectExists)
                throw new InvalidOperationException("Проект не найден или нет доступа");

            bool exists = await _context.ProjectTeams.AnyAsync(t =>
                t.ProjectId == model.ProjectId &&
                t.Name.ToLower() == normalizedName &&
                t.Project.OrganizationId == orgId &&
                !t.Project.IsArchived, ct);

            if (exists)
                throw new InvalidOperationException(DuplicateTeamNameMessage);

            var team = new ProjectTeam
            {
                ProjectId = model.ProjectId,
                Name = name,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                IsActive = true
            };

            _context.ProjectTeams.Add(team);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TeamId = team.Id,
                ProjectId = team.ProjectId,
                Action = "create",
                EntityType = "project_team",
                EntityId = team.Id,
                EntityName = team.Name,
                Title = "Создана команда",
                NewValues = new { team.Name, team.Description, team.ProjectId }
            }, ct);

            return team;
        }

        private async Task<(int ProjectId, string TeamName, string ProjectName)> GetTeamInfoOrThrow(int teamId, int orgId, CancellationToken ct)
        {
            var teamInfo = await _context.ProjectTeams
                .Where(t => t.Id == teamId && t.Project.OrganizationId == orgId && t.IsActive && !t.Project.IsArchived)
                .Select(t => new { t.ProjectId, t.Name, ProjectName = t.Project.Name })
                .FirstOrDefaultAsync(ct);

            if (teamInfo == null)
                throw new InvalidOperationException("Команда/проект не найдены или нет доступа");

            return (teamInfo.ProjectId, teamInfo.Name, teamInfo.ProjectName);
        }

        private async Task<(int MemberId, string FullName, int RoleId, string? Email)> GetMemberInfoOrThrow(int memberId, int orgId, CancellationToken ct)
        {
            var memberInfo = await _context.Members
                .Where(m => m.Id == memberId && m.OrganizationId == orgId)
                .Select(m => new
                {
                    m.Id,
                    m.Surname,
                    m.Name,
                    m.Patronymic,
                    m.RoleId,
                    Email = m.User != null ? m.User.CorporateEmail : null
                })
                .FirstOrDefaultAsync(ct);

            if (memberInfo == null)
                throw new InvalidOperationException("Участник не найден");

            var fullName = string.Join(" ",
                new[] { memberInfo.Surname, memberInfo.Name, memberInfo.Patronymic }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            return (memberInfo.Id, fullName, memberInfo.RoleId, memberInfo.Email);
        }

        private async Task<bool> IsMemberInAnotherTeamOfProject(int memberId, int projectId, int currentTeamId, CancellationToken ct)
        {
            return await (from ptm in _context.ProjectTeamMembers
                          join t in _context.ProjectTeams on ptm.TeamId equals t.Id
                          where ptm.MemberId == memberId
                                && t.ProjectId == projectId
                                && t.IsActive
                                && t.Id != currentTeamId
                          select 1)
                .AnyAsync(ct);
        }

        public async Task AddLeadToTeam(CreateLeadMebmerProjectDto model, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var lead = await GetMemberInfoOrThrow(model.MemberId, orgId, ct);

            if (lead.RoleId != (int)RoleEnum.TeamLead)
                throw new InvalidOperationException("Участник не является TeamLead");

            var (projectId, teamName, projectName) = await GetTeamInfoOrThrow(model.TeamId, orgId, ct);

            bool busy = await IsMemberInAnotherTeamOfProject(model.MemberId, projectId, model.TeamId, ct);
            if (busy)
                throw new InvalidOperationException("Руководитель уже назначен в другую команду этого проекта");

            var currentLeads = await _context.ProjectTeamMembers
                .Where(p => p.TeamId == model.TeamId && p.TeamRole == TeamLeadRoleTitle)
                .ToListAsync(ct);

            if (currentLeads.Any(p => p.MemberId == model.MemberId))
            {
                await tx.CommitAsync(ct);
                return;
            }

            var oldLeadIds = currentLeads.Select(p => p.MemberId).ToList();
            if (currentLeads.Count > 0)
                _context.ProjectTeamMembers.RemoveRange(currentLeads);

            var memberRow = await _context.ProjectTeamMembers
                .FirstOrDefaultAsync(p => p.TeamId == model.TeamId && p.MemberId == model.MemberId, ct);

            if (memberRow != null)
            {
                memberRow.TeamRole = TeamLeadRoleTitle;
            }
            else
            {
                _context.ProjectTeamMembers.Add(new ProjectTeamMember
                {
                    TeamId = model.TeamId,
                    MemberId = model.MemberId,
                    TeamRole = TeamLeadRoleTitle
                });
            }

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TeamId = model.TeamId,
                ProjectId = projectId,
                TargetMemberId = model.MemberId,
                Action = oldLeadIds.Count > 0 ? "update" : "create",
                EntityType = "project_team_member",
                EntityId = model.MemberId,
                EntityName = lead.FullName,
                Title = oldLeadIds.Count > 0 ? "Руководитель команды переназначен" : "Назначен руководитель команды",
                OldValues = oldLeadIds.Count > 0 ? new { LeadMemberIds = oldLeadIds } : null,
                NewValues = new { model.TeamId, model.MemberId, TeamRole = TeamLeadRoleTitle }
            }, ct);

            if (!string.IsNullOrWhiteSpace(lead.Email))
            {
                await _emailHelper.SendTeamAssignmentAsync(lead.Email, new TeamAssignmentEmailDto
                {
                    RecipientFullName = lead.FullName,
                    TeamName = teamName,
                    ProjectName = projectName,
                    RoleTitle = TeamLeadRoleTitle
                });
            }
        }

        public async Task AddMemberToTeam(CreateMebmerProjectDto model, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var (projectId, teamName, projectName) = await GetTeamInfoOrThrow(model.TeamId, orgId, ct);
            var member = await GetMemberInfoOrThrow(model.MemberId, orgId, ct);

            if (member.RoleId == (int)RoleEnum.TeamLead)
                throw new InvalidOperationException("Нельзя добавить TeamLead как обычного участника");

            bool busy = await IsMemberInAnotherTeamOfProject(model.MemberId, projectId, model.TeamId, ct);
            if (busy)
                throw new InvalidOperationException("Участник уже состоит в другой команде этого проекта");

            bool alreadyInThisTeam = await _context.ProjectTeamMembers
                .AnyAsync(p => p.TeamId == model.TeamId && p.MemberId == model.MemberId, ct);

            if (alreadyInThisTeam)
                throw new InvalidOperationException("Участник уже состоит в команде!");

            _context.ProjectTeamMembers.Add(new ProjectTeamMember
            {
                TeamId = model.TeamId,
                MemberId = model.MemberId,
                TeamRole = model.TeamRole
            });

            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TeamId = model.TeamId,
                ProjectId = projectId,
                TargetMemberId = model.MemberId,
                Action = "create",
                EntityType = "project_team_member",
                EntityId = model.MemberId,
                EntityName = member.FullName,
                Title = "Участник добавлен в команду",
                NewValues = new { model.TeamId, model.MemberId, model.TeamRole }
            }, ct);

            if (!string.IsNullOrWhiteSpace(member.Email))
            {
                await _emailHelper.SendTeamAssignmentAsync(member.Email, new TeamAssignmentEmailDto
                {
                    RecipientFullName = member.FullName,
                    TeamName = teamName,
                    ProjectName = projectName,
                    RoleTitle = string.IsNullOrWhiteSpace(model.TeamRole) ? "Участник" : model.TeamRole
                });
            }
        }

        public async Task UpdateTeamRole(UpdateTeamRoleDto model, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var row = await _context.ProjectTeamMembers
                .Include(x => x.Team)
                .FirstOrDefaultAsync(x =>
                    x.MemberId == model.MemberId &&
                    x.Team.IsActive &&
                    !x.Team.Project.IsArchived &&
                    x.Team.Project.OrganizationId == orgId, ct);

            if (row == null)
                throw new InvalidOperationException("Участник не найден в этой команде");

            var oldRole = row.TeamRole;
            row.TeamRole = model.RoleName;
            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TeamId = row.TeamId,
                ProjectId = row.Team.ProjectId,
                TargetMemberId = row.MemberId,
                Action = "update",
                EntityType = "project_team_member",
                EntityId = row.MemberId,
                Title = "Обновлена роль участника в команде",
                OldValues = new { TeamRole = oldRole },
                NewValues = new { row.TeamRole }
            }, ct);
        }

        public async Task DeleteMebmerProject(int memberId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var row = await _context.ProjectTeamMembers
                .Include(x => x.Team)
                .FirstOrDefaultAsync(x =>
                    x.MemberId == memberId &&
                    x.Team.IsActive &&
                    !x.Team.Project.IsArchived &&
                    x.Team.Project.OrganizationId == orgId, ct);

            if (row == null)
                throw new InvalidOperationException("Участник не найден в этой команде");

            _context.ProjectTeamMembers.Remove(row);
            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TeamId = row.TeamId,
                ProjectId = row.Team.ProjectId,
                TargetMemberId = row.MemberId,
                Action = "delete",
                EntityType = "project_team_member",
                EntityId = row.MemberId,
                Title = "Участник удален из команды",
                OldValues = new { row.TeamId, row.MemberId, row.TeamRole }
            }, ct);
        }

        public async Task DeleteMemberFromTeam(int teamId, int memberId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var row = await _context.ProjectTeamMembers
                .Include(x => x.Team)
                .FirstOrDefaultAsync(x =>
                    x.TeamId == teamId &&
                    x.MemberId == memberId &&
                    x.Team.IsActive &&
                    !x.Team.Project.IsArchived &&
                    x.Team.Project.OrganizationId == orgId, ct);

            if (row == null)
                throw new InvalidOperationException("Участник не найден в этой команде");

            _context.ProjectTeamMembers.Remove(row);
            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TeamId = row.TeamId,
                ProjectId = row.Team.ProjectId,
                TargetMemberId = row.MemberId,
                Action = "delete",
                EntityType = "project_team_member",
                EntityId = row.MemberId,
                Title = "Участник удален из команды",
                OldValues = new { row.TeamId, row.MemberId, row.TeamRole }
            }, ct);
        }

        public async Task DeleteTeam(int teamId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            var now = UtcNowTimestamp();

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var team = await _context.ProjectTeams
                .FirstOrDefaultAsync(t =>
                    t.Id == teamId &&
                    t.Project.OrganizationId == orgId &&
                    t.IsActive, ct);

            if (team == null)
                throw new InvalidOperationException("Команда не найдена");

            team.IsActive = false;
            team.UpdatedAt = now;

            await _context.ProjectTasks
                .Where(t => t.TeamId == teamId && t.Project.OrganizationId == orgId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.IsArchived, true)
                    .SetProperty(t => t.UpdatedAt, now), ct);

            await _context.ProjectTeamMembers
                .Where(ptm => ptm.TeamId == teamId && ptm.Team.Project.OrganizationId == orgId)
                .ExecuteDeleteAsync(ct);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogAsync(_userHelper.GetCurrentUserId(), new AuditLogCreateDto
            {
                TeamId = team.Id,
                ProjectId = team.ProjectId,
                Action = "delete",
                EntityType = "project_team",
                EntityId = team.Id,
                EntityName = team.Name,
                Title = "Команда удалена",
                OldValues = new { IsActive = true },
                NewValues = new { team.IsActive }
            }, ct);
        }

        public async Task<List<MemberDto>> GetProjectLeadsWithoutTeam(int projectId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var rows = await _context.Members
                .AsNoTracking()
                .Where(m =>
                    m.OrganizationId == orgId &&
                    m.IsActive &&
                    m.RoleId == (int)RoleEnum.TeamLead &&
                    !m.ProjectTeamMembers.Any(ptm =>
                        ptm.Team.ProjectId == projectId &&
                        ptm.Team.Project.OrganizationId == orgId &&
                        ptm.Team.IsActive &&
                        !ptm.Team.Project.IsArchived)
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


        public async Task<List<MemberDto>> GetMembersWithoutTeam(int projectId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var rows = await _context.Members
                .AsNoTracking()
                .Where(m =>
                    m.OrganizationId == orgId &&
                    m.IsActive &&
                    m.RoleId == (int)RoleEnum.Employee &&
                    !m.ProjectTeamMembers.Any(ptm =>
                        ptm.Team.ProjectId == projectId &&
                        ptm.Team.Project.OrganizationId == orgId &&
                        ptm.Team.IsActive &&
                        !ptm.Team.Project.IsArchived)
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

        public async Task<List<TeamReadDto>> GetByProject(int projectId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var rows = await _context.ProjectTeams
                .AsNoTracking()
                .Where(t =>
                    t.ProjectId == projectId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.ProjectId,
                    t.Name,
                    t.Description,
                    t.CardBackgroundUrl,
                    t.IsActive,
                    t.CreatedAt,
                    t.UpdatedAt,
                    Lead = t.ProjectTeamMembers
                        .Where(ptm => ptm.TeamRole == TeamLeadRoleTitle)
                        .Select(ptm => new
                        {
                            ptm.MemberId,
                            ptm.Member.Surname,
                            ptm.Member.Name,
                            ptm.Member.Patronymic,
                            ptm.Member.AvatarUrl
                        })
                        .FirstOrDefault()
                })
                .OrderBy(t => t.Name)
                .ToListAsync(ct);

            return rows.Select(t => new TeamReadDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Name = t.Name,
                Description = t.Description,
                CardBackgroundUrl = t.CardBackgroundUrl,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                LeadMemberId = t.Lead?.MemberId,
                LeadFullName = t.Lead == null
                    ? null
                    : BuildFullName(t.Lead.Surname, t.Lead.Name, t.Lead.Patronymic),
                LeadAvatarUrl = t.Lead?.AvatarUrl
            }).ToList();
        }

        public async Task UpdateTeam(int teamId, UpdateTeamDto model, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();
            var name = (model.Name ?? string.Empty).Trim();
            var normalizedName = name.ToLower();

            var team = await _context.ProjectTeams
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t =>
                    t.Id == teamId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    t.IsActive, ct);

            if (team == null)
                throw new InvalidOperationException("Команда не найдена");

            var nameIsTaken = await _context.ProjectTeams.AnyAsync(t =>
                t.Id != team.Id &&
                t.ProjectId == team.ProjectId &&
                t.Name.ToLower() == normalizedName &&
                t.Project.OrganizationId == orgId &&
                !t.Project.IsArchived, ct);

            if (nameIsTaken)
                throw new InvalidOperationException(DuplicateTeamNameMessage);

            team.Name = name;
            team.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
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
                Title = "Обновлена команда",
                NewValues = new { team.Name, team.Description }
            }, ct);
        }

        public async Task<TeamReadDto> GetTeam(int teamId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var row = await _context.ProjectTeams
                .AsNoTracking()
                .Where(t =>
                    t.Id == teamId &&
                    t.Project.OrganizationId == orgId &&
                    !t.Project.IsArchived &&
                    t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.ProjectId,
                    t.Name,
                    t.Description,
                    t.CardBackgroundUrl,
                    t.IsActive,
                    t.CreatedAt,
                    t.UpdatedAt,
                    Lead = t.ProjectTeamMembers
                        .Where(ptm => ptm.TeamRole == TeamLeadRoleTitle)
                        .Select(ptm => new
                        {
                            ptm.MemberId,
                            ptm.Member.Surname,
                            ptm.Member.Name,
                            ptm.Member.Patronymic,
                            ptm.Member.AvatarUrl
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);

            if (row == null)
                throw new InvalidOperationException("Команда не найдена");

            return new TeamReadDto
            {
                Id = row.Id,
                ProjectId = row.ProjectId,
                Name = row.Name,
                Description = row.Description,
                CardBackgroundUrl = row.CardBackgroundUrl,
                IsActive = row.IsActive,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAt,
                LeadMemberId = row.Lead?.MemberId,
                LeadFullName = row.Lead == null
                    ? null
                    : BuildFullName(row.Lead.Surname, row.Lead.Name, row.Lead.Patronymic),
                LeadAvatarUrl = row.Lead?.AvatarUrl
            };
        }

        public async Task<List<TeamMemberDto>> GetTeamMembers(int teamId, CancellationToken ct = default)
        {
            int orgId = _userHelper.GetCurrentOrganizationId();

            var rows = await _context.ProjectTeamMembers
                .AsNoTracking()
                .Where(ptm =>
                    ptm.TeamId == teamId &&
                    ptm.Team.Project.OrganizationId == orgId &&
                    !ptm.Team.Project.IsArchived &&
                    ptm.Team.IsActive &&
                    ptm.Member.IsActive)
                .Select(ptm => new
                {
                    ptm.MemberId,
                    ptm.Member.Surname,
                    ptm.Member.Name,
                    ptm.Member.Patronymic,
                    RoleName = ptm.Member.Role.Name,
                    ptm.TeamRole,
                    ptm.Member.IsActive,
                    ptm.Member.AvatarUrl
                })
                .OrderBy(x => x.Surname)
                .ToListAsync(ct);

            return rows.Select(m => new TeamMemberDto
            {
                MemberId = m.MemberId,
                FullName = BuildFullName(m.Surname, m.Name, m.Patronymic),
                RoleName = m.RoleName,
                TeamRole = m.TeamRole,
                IsActive = m.IsActive,
                AvatarUrl = m.AvatarUrl
            }).ToList();
        }

        private static string BuildFullName(string surname, string name, string? patronymic)
        {
            if (string.IsNullOrWhiteSpace(patronymic))
                return $"{surname} {name}";
            return $"{surname} {name} {patronymic}";
        }
    }
}
