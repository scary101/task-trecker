using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.WorkSessionDTOs;
using steptreck.Domain.Enums;

namespace steptreck.API.Services.SessonWork
{
    public class WorkSessionService
    {
        private readonly AppDbContext _context;
        private readonly UserHelper _userHelper;
        private readonly AuditService _auditService;



        public WorkSessionService(AppDbContext context, UserHelper userHelper, AuditService auditService)
        {
            _context = context;
            _userHelper = userHelper;
            _auditService = auditService;
        }


        public async Task StartSessionAsync()
        {
            var userId = _userHelper.GetCurrentUserId();
            var orgId = _userHelper.GetCurrentOrganizationId();

            Console.WriteLine($"[StartSession] START userId={userId}, orgId={orgId}");

            var member = await _context.Members
                .Include(x => x.ProjectTeamMembers)
                    .ThenInclude(x => x.Team)
                        .ThenInclude(x => x.Project)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.OrganizationId == orgId);

            Console.WriteLine($"[StartSession] member found = {member != null}");

            if (member is null)
                throw new Exception("Участник не найден");

            Console.WriteLine($"[StartSession] member.RoleId = {member.RoleId}");
            Console.WriteLine($"[StartSession] ProjectTeamMembers count = {member.ProjectTeamMembers.Count}");

            foreach (var ptm in member.ProjectTeamMembers)
            {
                Console.WriteLine($@"
[StartSession] TeamMember:
    TeamId = {ptm.TeamId}
    Team exists = {ptm.Team != null}
    Team.IsActive = {ptm.Team?.IsActive}
    Project exists = {ptm.Team?.Project != null}
    ProjectId = {ptm.Team?.ProjectId}
    Project.IsArchived = {ptm.Team?.Project?.IsArchived}
");
            }

            var teamMember = member.ProjectTeamMembers
                .Where(x => x.Team != null)
                .Where(x => x.Team.IsActive)
                .Where(x => x.Team.Project != null && !x.Team.Project.IsArchived)
                .FirstOrDefault();

            Console.WriteLine($"[StartSession] valid teamMember found = {teamMember != null}");

            if (teamMember != null)
            {
                Console.WriteLine($@"
[StartSession] Selected team:
    TeamId = {teamMember.TeamId}
    ProjectId = {teamMember.Team?.ProjectId}
");
            }

            var now = DateTime.UtcNow;

            var existingStatus = await _context.EmployeeWorkStatuses
                .FirstOrDefaultAsync(x => x.UserId == userId && x.OrgId == orgId);

            Console.WriteLine($"[StartSession] existingStatus exists = {existingStatus != null}");

            if (existingStatus != null)
            {
                Console.WriteLine($@"
[StartSession] Existing status:
    CurrentStatus = {existingStatus.CurrentStatus}
    CurrentSessionId = {existingStatus.CurrentSessionId}
");
            }

            if (existingStatus != null && existingStatus.CurrentStatus != (int)WorkStatus.Offline)
            {
                Console.WriteLine("[StartSession] Session already active");
                throw new Exception("Сессия уже активна");
            }

            var session = new WorkSession
            {
                UserId = userId,
                OrgId = orgId,
                StartedAtUtc = now,
                Status = (int)WorkStatus.Working,
                DurationSeconds = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                TeamId = teamMember?.TeamId,
                ProjectId = teamMember?.Team?.ProjectId
            };

            Console.WriteLine($@"
[StartSession] Creating session:
    UserId = {session.UserId}
    OrgId = {session.OrgId}
    TeamId = {session.TeamId}
    ProjectId = {session.ProjectId}
");

            try
            {
                _context.WorkSessions.Add(session);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[StartSession] Session created. sessionId={session.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StartSession] ERROR while saving WorkSession");
                Console.WriteLine(ex);
                throw;
            }

            try
            {
                if (existingStatus == null)
                {
                    Console.WriteLine("[StartSession] Creating EmployeeWorkStatus");

                    var newStatus = new EmployeeWorkStatus
                    {
                        UserId = userId,
                        OrgId = orgId,
                        CurrentStatus = (int)WorkStatus.Working,
                        CurrentSessionId = session.Id,
                        StatusStartedAtUtc = now,
                        UpdatedAtUtc = now
                    };

                    _context.EmployeeWorkStatuses.Add(newStatus);
                }
                else
                {
                    Console.WriteLine("[StartSession] Updating EmployeeWorkStatus");

                    existingStatus.CurrentStatus = (int)WorkStatus.Working;
                    existingStatus.CurrentSessionId = session.Id;
                    existingStatus.StatusStartedAtUtc = now;
                    existingStatus.UpdatedAtUtc = now;
                }

                await _context.SaveChangesAsync();

                Console.WriteLine("[StartSession] EmployeeWorkStatus saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StartSession] ERROR while saving EmployeeWorkStatus");
                Console.WriteLine(ex);
                throw;
            }

            try
            {
                Console.WriteLine("[StartSession] Writing audit log");

                await _auditService.LogWithAllIdAsync(orgId, member.Id, new AuditLogCreateDto
                {
                    ProjectId = session.ProjectId,
                    TeamId = session.TeamId,
                    Action = "create",
                    EntityType = "work_session",
                    EntityId = session.Id,
                    Title = "Рабочая сессия начата",
                    NewValues = new { session.StartedAtUtc, session.Status }
                });

                Console.WriteLine("[StartSession] Audit log saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StartSession] ERROR while writing audit");
                Console.WriteLine(ex);
                throw;
            }

            Console.WriteLine("[StartSession] SUCCESS");
        }

        public async Task ToogleSessionAsync()
        {
            var userId = _userHelper.GetCurrentUserId();
            var orgId = _userHelper.GetCurrentOrganizationId();
            var now = DateTime.UtcNow;

            var session = await _context.EmployeeWorkStatuses
                .Include(s => s.CurrentSession)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.OrgId == orgId);

            if (session == null || session.CurrentSession == null)
            {
                throw new Exception("Ошибка переключения состояния");
            }

            if (session.CurrentStatus == (int)WorkStatus.Working)
            {
                var workedSeconds = (int)(now - session.StatusStartedAtUtc).TotalSeconds;
                session.CurrentSession.DurationSeconds += workedSeconds;

                session.CurrentStatus = (int)WorkStatus.Paused;
                session.CurrentSession.Status = (int)WorkStatus.Paused;
                session.StatusStartedAtUtc = now;
            }
            else if (session.CurrentStatus == (int)WorkStatus.Paused)
            {
                session.CurrentStatus = (int)WorkStatus.Working;
                session.CurrentSession.Status = (int)WorkStatus.Working;
                session.StatusStartedAtUtc = now;
            }

            session.UpdatedAtUtc = now;
            session.CurrentSession.UpdatedAtUtc = now;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userId, new AuditLogCreateDto
            {
                ProjectId = session.CurrentSession.ProjectId,
                TeamId = session.CurrentSession.TeamId,
                Action = "update",
                EntityType = "work_session",
                EntityId = session.CurrentSession.Id,
                Title = session.CurrentStatus == (int)WorkStatus.Paused
                    ? "Рабочая сессия поставлена на паузу"
                    : "Рабочая сессия продолжена",
                NewValues = new { session.CurrentStatus, session.CurrentSession.DurationSeconds }
            });
        }

        public async Task StopSession()
        {
            var userId = _userHelper.GetCurrentUserId();
            var orgId = _userHelper.GetCurrentOrganizationId();
            var now = DateTime.UtcNow;

            var session = await _context.EmployeeWorkStatuses
                .Include(s => s.CurrentSession)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.OrgId == orgId);

            if (session == null || session.CurrentSession == null)
            {
                throw new Exception("Ошибка остановки сессии");
            }

            if (session.CurrentStatus == (int)WorkStatus.Working)
            {
                var workedSeconds = (int)(now - session.StatusStartedAtUtc).TotalSeconds;
                session.CurrentSession.DurationSeconds += workedSeconds;
            }

            session.CurrentStatus = (int)WorkStatus.Offline;
            session.CurrentSession.Status = (int)WorkStatus.Completed;

            session.CurrentSession.EndedAtUtc = now;
            session.CurrentSession.UpdatedAtUtc = now;

            session.CurrentSessionId = null;
            session.StatusStartedAtUtc = now;
            session.UpdatedAtUtc = now;

            await _context.SaveChangesAsync();

            try
            {
                await _auditService.LogAsync(userId, new AuditLogCreateDto
                {
                    ProjectId = session.CurrentSession.ProjectId,
                    TeamId = session.CurrentSession.TeamId,
                    Action = "update",
                    EntityType = "work_session",
                    EntityId = session.CurrentSession.Id,
                    Title = "Рабочая сессия завершена",
                    NewValues = new
                    {
                        session.CurrentSession.Status,
                        session.CurrentSession.EndedAtUtc,
                        session.CurrentSession.DurationSeconds
                    }
                });
            }
            catch
            {
                // Сессия уже завершена; ошибка аудита не должна ломать ответ для таймера.
            }
        }

        public async Task<List<ActiveEmployeeSessionDto>> GetCurrentEmployeeSessionsAsync()
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            return await GetCurrentEmployeeSessionsAsync(orgId);
        }

        public async Task<List<WorkSessionHistoryDto>> GetCompletedSessionsAsync()
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            return await GetCompletedSessionsAsync(orgId);
        }

        public async Task<List<ActiveEmployeeSessionDto>> GetCurrentEmployeeSessionsProjectAsync(int projectId)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            return await GetCurrentEmployeeSessionsAsync(orgId, projectId: projectId);
        }

        public async Task<List<WorkSessionHistoryDto>> GetCompletedSessionsProjectAsync(int projectId)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            return await GetCompletedSessionsAsync(orgId, projectId: projectId);
        }

        public async Task<List<ActiveEmployeeSessionDto>> GetCurrentEmployeeSessionsTeamAsync(int teamId)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            return await GetCurrentEmployeeSessionsAsync(orgId, teamId: teamId);
        }

        public async Task<List<WorkSessionHistoryDto>> GetCompletedSessionsTeamAsync(int teamId)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            return await GetCompletedSessionsAsync(orgId, teamId: teamId);
        }


        public async Task<UserCurrentSessionDto?> GetUserCurrentOrLastSessionAsync(int userId)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            var activeStatus = await _context.EmployeeWorkStatuses
                .AsNoTracking()
                .Include(x => x.CurrentSession)
                .FirstOrDefaultAsync(x =>
                    x.OrgId == orgId &&
                    x.UserId == userId);

            if (activeStatus?.CurrentSession != null)
            {
                return new UserCurrentSessionDto
                {
                    SessionId = activeStatus.CurrentSession.Id,
                    UserId = userId,
                    Status = activeStatus.CurrentSession.Status,
                    StartedAtUtc = activeStatus.CurrentSession.StartedAtUtc,
                    EndedAtUtc = activeStatus.CurrentSession.EndedAtUtc,
                    DurationSeconds = activeStatus.CurrentSession.DurationSeconds
                };
            }

            var lastSession = await _context.WorkSessions
                .AsNoTracking()
                .Where(x =>
                    x.OrgId == orgId &&
                    x.UserId == userId)
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstOrDefaultAsync();

            if (lastSession == null)
                return null;

            return new UserCurrentSessionDto
            {
                SessionId = lastSession.Id,
                UserId = userId,
                Status = lastSession.Status,
                StartedAtUtc = lastSession.StartedAtUtc,
                EndedAtUtc = lastSession.EndedAtUtc,
                DurationSeconds = lastSession.DurationSeconds
            };
        }

        public async Task<List<WorkSessionHistoryDto>> GetUserCompletedSessionsAsync(int userId)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();

            return await _context.WorkSessions
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x =>
                    x.OrgId == orgId &&
                    x.UserId == userId &&
                    x.Status == (int)WorkStatus.Completed)
                .OrderByDescending(x => x.StartedAtUtc)
                .Select(x => new WorkSessionHistoryDto
                {
                    SessionId = x.Id,
                    UserId = x.UserId,
                    UserName = x.User.Name + " " + x.User.Surname + " " + x.User.Patronymic,
                    StartedAtUtc = x.StartedAtUtc,
                    EndedAtUtc = x.EndedAtUtc,
                    DurationSeconds = x.DurationSeconds,
                    Status = x.Status
                })
                .ToListAsync();
        }

        public async Task<List<WorkSessionHistoryDto>> GetMyCompletedSessionsAsync()
        {
            var userId = _userHelper.GetCurrentUserId();
            return await GetUserCompletedSessionsAsync(userId);
        }

        private async Task<List<ActiveEmployeeSessionDto>> GetCurrentEmployeeSessionsAsync(int orgId, int? projectId = null, int? teamId = null)
        {
            var currentUserId = _userHelper.GetCurrentUserId();
            var currentRoleId = await _context.Members
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.UserId == currentUserId && m.IsActive)
                .Select(m => (int?)m.RoleId)
                .FirstOrDefaultAsync();
            var isProjectManager = currentRoleId == (int)RoleEnum.ProjectManaget;

            var query = _context.EmployeeWorkStatuses
                .AsNoTracking()
                .Where(x =>
                    x.OrgId == orgId &&
                    x.CurrentSessionId != null &&
                    (x.CurrentStatus == (int)WorkStatus.Working ||
                     x.CurrentStatus == (int)WorkStatus.Paused));

            if (isProjectManager)
            {
                query = query.Where(x => x.UserId == currentUserId);
            }
            else
            {
                query = query.Where(x =>
                    x.CurrentSession!.Project != null &&
                    !x.CurrentSession.Project.IsArchived &&
                    x.CurrentSession.Team != null &&
                    x.CurrentSession.Team.IsActive &&
                    !x.User.Members.Any(m =>
                        m.OrganizationId == orgId &&
                        m.RoleId == (int)RoleEnum.ProjectManaget &&
                        m.IsActive));
            }

            if (projectId.HasValue)
            {
                query = query.Where(x => x.CurrentSessionId != null && x.CurrentSession!.ProjectId == projectId.Value);
            }

            if (teamId.HasValue)
            {
                query = query.Where(x => x.CurrentSessionId != null && x.CurrentSession!.TeamId == teamId.Value);
            }

            return await query
                .Select(x => new ActiveEmployeeSessionDto
                {
                    UserId = x.UserId,
                    UserName = x.User.Name + " " + x.User.Surname + " " + x.User.Patronymic,
                    CurrentStatus = x.CurrentStatus,
                    StatusStartedAtUtc = x.StatusStartedAtUtc,
                    SessionId = x.CurrentSessionId,
                    SessionStartedAtUtc = x.CurrentSession != null
                        ? x.CurrentSession.StartedAtUtc
                        : null,
                    DurationSeconds = x.CurrentSession != null
                        ? x.CurrentSession.DurationSeconds
                        : 0
                })
                .ToListAsync();
        }

        private async Task<List<WorkSessionHistoryDto>> GetCompletedSessionsAsync(int orgId, int? projectId = null, int? teamId = null)
        {
            var query = _context.WorkSessions
                .AsNoTracking()
                .Where(x =>
                    x.OrgId == orgId &&
                    x.Project != null &&
                    !x.Project.IsArchived &&
                    x.Team != null &&
                    x.Team.IsActive &&
                    x.Status == (int)WorkStatus.Completed);

            if (projectId.HasValue)
            {
                query = query.Where(x => x.ProjectId == projectId.Value);
            }

            if (teamId.HasValue)
            {
                query = query.Where(x => x.TeamId == teamId.Value);
            }

            return await query
                .OrderByDescending(x => x.StartedAtUtc)
                .Select(x => new WorkSessionHistoryDto
                {
                    SessionId = x.Id,
                    UserId = x.UserId,
                    UserName = x.User.Name + " " + x.User.Surname + " " + x.User.Patronymic,
                    StartedAtUtc = x.StartedAtUtc,
                    EndedAtUtc = x.EndedAtUtc,
                    DurationSeconds = x.DurationSeconds,
                    Status = x.Status
                })
                .ToListAsync();
        }



    }
}
