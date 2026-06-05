using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain;
using steptreck.Domain.DTOs;
using System.Text.Json;

public class AuditService
{
    private readonly AppDbContext _context;
    private readonly UserHelper _userHelper;
    private readonly IHttpContextAccessor _http;

    public AuditService(AppDbContext context, UserHelper userHelper, IHttpContextAccessor http)
    {
        _context = context;
        _userHelper = userHelper;
        _http = http;
    }

    public async Task<List<steptreck.API.Models.AuditLog>> GetLogsAsync(string tableName = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(tableName))
            query = query.Where(x => x.TableName == tableName);

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task LogAsync(int userId, AuditLogCreateDto dto, CancellationToken ct = default)
    {
        int orgId = _userHelper.GetCurrentOrganizationId();
        int? actorMemberId = await GetActorMemberIdAsync(userId, orgId, ct);
        await AddAppAuditLogAsync(orgId, actorMemberId, dto, ct);
    }

    public async Task LogWithIdAsync(int userId, int orgId, AuditLogCreateDto dto, CancellationToken ct = default)
    {
        int? actorMemberId = await GetActorMemberIdAsync(userId, orgId, ct);
        await AddAppAuditLogAsync(orgId, actorMemberId, dto, ct);
    }

    public async Task LogWithAllIdAsync(int orgId, int actorMemberId, AuditLogCreateDto dto, CancellationToken ct = default)
    {
        await AddAppAuditLogAsync(orgId, actorMemberId, dto, ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetAllLogsAsync(
        int page,
        int pageSize,
        string? search,
        string? action,
        string? actor,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        bool sortDesc,
        CancellationToken ct)
    {
        int orgId = _userHelper.GetCurrentOrganizationId();
        var query = _context.AppAuditLogs.AsNoTracking();

        return await BuildLogsAsync(query, orgId, page, pageSize, search, action, actor, dateFrom, dateTo, sortBy, sortDesc, ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetProjectLogsAsync(
        int projectId,
        int page,
        int pageSize,
        string? action,
        string? actor,
        string? dateFrom,
        string? dateTo,
        CancellationToken ct)
    {
        int orgId = _userHelper.GetCurrentOrganizationId();

        var query = _context.AppAuditLogs
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId);

        return await BuildLogsAsync(query, orgId, page, pageSize, null, action, actor, dateFrom, dateTo, null, true, ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetTeamLogsAsync(
        int teamId,
        int page,
        int pageSize,
        string? action,
        string? actor,
        string? dateFrom,
        string? dateTo,
        CancellationToken ct)
    {
        int orgId = _userHelper.GetCurrentOrganizationId();

        var query = _context.AppAuditLogs
            .AsNoTracking()
            .Where(x => x.TeamId == teamId);

        return await BuildLogsAsync(query, orgId, page, pageSize, null, action, actor, dateFrom, dateTo, null, true, ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetMemberLogsAsync(
        int memberId,
        int page,
        int pageSize,
        string? action,
        string? actor,
        string? dateFrom,
        string? dateTo,
        CancellationToken ct)
    {
        int orgId = _userHelper.GetCurrentOrganizationId();

        var query = _context.AppAuditLogs
            .AsNoTracking()
            .Where(x => x.ActorMemberId == memberId || x.TargetMemberId == memberId);

        return await BuildLogsAsync(query, orgId, page, pageSize, null, action, actor, dateFrom, dateTo, null, true, ct);
    }

    private async Task<PagedResult<AuditLogDto>> BuildLogsAsync(
        IQueryable<AppAuditLog> query,
        int orgId,
        int page,
        int pageSize,
        string? search,
        string? action,
        string? actor,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        bool sortDesc,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filtered = query.Where(x => x.OrganizationId == orgId);

        filtered = ApplyActionFilter(filtered, action);
        filtered = ApplyDateFilter(filtered, dateFrom, dateTo);
        filtered = await ApplyActorFilterAsync(filtered, orgId, actor, ct);
        filtered = await ApplySearchAsync(filtered, orgId, search, ct);
        filtered = ApplySort(filtered, sortBy, sortDesc);

        var total = await filtered.CountAsync(ct);

        var rows = await filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var actorIds = rows
            .Where(x => x.ActorMemberId.HasValue)
            .Select(x => x.ActorMemberId!.Value)
            .Distinct()
            .ToList();

        var actors = await _context.Members
            .AsNoTracking()
            .Where(m => m.OrganizationId == orgId && actorIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                FullName = m.Surname + " " + m.Name
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var items = rows.Select(x =>
        {
            actors.TryGetValue(x.ActorMemberId ?? 0, out var actorRow);

            return new AuditLogDto
            {
                Id = x.Id,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                EntityName = x.EntityName,
                Title = x.Title,
                Description = x.Description,
                ActorMemberId = x.ActorMemberId,
                ActorName = actorRow?.FullName,
                TargetMemberId = x.TargetMemberId,
                TeamId = x.TeamId,
                ProjectId = x.ProjectId,
                TaskId = x.TaskId,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                CreatedAt = x.CreatedAt
            };
        }).ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static IQueryable<AppAuditLog> ApplyActionFilter(IQueryable<AppAuditLog> query, string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return query;

        var value = action.Trim().ToLowerInvariant();

        return value switch
        {
            "create" => query.Where(x => x.Action.ToLower() == "create" || x.Action.ToLower() == "insert"),
            "update" => query.Where(x => x.Action.ToLower() == "update"),
            "delete" => query.Where(x => x.Action.ToLower() == "delete"),
            _ => query.Where(x => x.Action.ToLower() == value)
        };
    }

    private static IQueryable<AppAuditLog> ApplyDateFilter(
        IQueryable<AppAuditLog> query,
        string? dateFrom,
        string? dateTo)
    {
        if (DateTime.TryParse(dateFrom, out var from))
            query = query.Where(x => x.CreatedAt >= from.Date);

        if (DateTime.TryParse(dateTo, out var to))
            query = query.Where(x => x.CreatedAt < to.Date.AddDays(1));

        return query;
    }

    private async Task<IQueryable<AppAuditLog>> ApplyActorFilterAsync(
        IQueryable<AppAuditLog> query,
        int orgId,
        string? actor,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor))
            return query;

        var term = actor.Trim().ToLowerInvariant();

        var actorIds = await _context.Members
            .AsNoTracking()
            .Where(m =>
                m.OrganizationId == orgId &&
                (
                    ((m.Surname ?? "") + " " + (m.Name ?? "") + " " + (m.Patronymic ?? "")).ToLower().Contains(term) ||
                    ((m.Name ?? "") + " " + (m.Surname ?? "")).ToLower().Contains(term)
                ))
            .Select(m => m.Id)
            .ToListAsync(ct);

        return query.Where(x => x.ActorMemberId.HasValue && actorIds.Contains(x.ActorMemberId.Value));
    }

    private async Task<IQueryable<AppAuditLog>> ApplySearchAsync(
        IQueryable<AppAuditLog> query,
        int orgId,
        string? search,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim().ToLowerInvariant();

        var actorIds = await _context.Members
            .AsNoTracking()
            .Where(m =>
                m.OrganizationId == orgId &&
                (
                    ((m.Surname ?? "") + " " + (m.Name ?? "") + " " + (m.Patronymic ?? "")).ToLower().Contains(term) ||
                    ((m.Name ?? "") + " " + (m.Surname ?? "")).ToLower().Contains(term)
                ))
            .Select(m => m.Id)
            .ToListAsync(ct);

        var hasEntityId = long.TryParse(term, out var entityId);

        return query.Where(x =>
            x.Action.ToLower().Contains(term) ||
            x.EntityType.ToLower().Contains(term) ||
            (x.EntityName != null && x.EntityName.ToLower().Contains(term)) ||
            (x.Title != null && x.Title.ToLower().Contains(term)) ||
            (x.Description != null && x.Description.ToLower().Contains(term)) ||
            (x.ActorMemberId.HasValue && actorIds.Contains(x.ActorMemberId.Value)) ||
            (hasEntityId && x.EntityId == entityId));
    }

    private static IQueryable<AppAuditLog> ApplySort(
        IQueryable<AppAuditLog> query,
        string? sortBy,
        bool sortDesc)
    {
        var key = (sortBy ?? "date").Trim().ToLowerInvariant();

        return key switch
        {
            "action" => sortDesc
                ? query.OrderByDescending(x => x.Action).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.Action).ThenByDescending(x => x.CreatedAt),

            "entity" => sortDesc
                ? query.OrderByDescending(x => x.EntityType).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.EntityType).ThenByDescending(x => x.CreatedAt),

            "title" => sortDesc
                ? query.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.Title).ThenByDescending(x => x.CreatedAt),

            "project" => sortDesc
                ? query.OrderByDescending(x => x.ProjectId).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.ProjectId).ThenByDescending(x => x.CreatedAt),

            "team" => sortDesc
                ? query.OrderByDescending(x => x.TeamId).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.TeamId).ThenByDescending(x => x.CreatedAt),

            "task" => sortDesc
                ? query.OrderByDescending(x => x.TaskId).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.TaskId).ThenByDescending(x => x.CreatedAt),

            _ => sortDesc
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
        };
    }

    private async Task<int?> GetActorMemberIdAsync(int userId, int orgId, CancellationToken ct)
    {
        try
        {
            return await _context.Members
                .Where(m => m.OrganizationId == orgId && m.UserId == userId)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task AddAppAuditLogAsync(int orgId, int? actorMemberId, AuditLogCreateDto dto, CancellationToken ct)
    {
        var log = new AppAuditLog
        {
            OrganizationId = orgId,
            TeamId = dto.TeamId,
            ProjectId = dto.ProjectId,
            TaskId = dto.TaskId,
            ActorMemberId = actorMemberId,
            TargetMemberId = dto.TargetMemberId,
            Action = dto.Action,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            EntityName = dto.EntityName,
            Title = dto.Title,
            Description = dto.Description,
            OldValues = Serialize(dto.OldValues),
            NewValues = Serialize(dto.NewValues),
            Metadata = Serialize(dto.Metadata),
            IpAddress = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = _http.HttpContext?.Request?.Headers["User-Agent"].ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.AppAuditLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }

    private static string? Serialize(object? obj)
    {
        if (obj == null)
            return null;

        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}