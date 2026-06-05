using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.API.Services.Chat;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.AuthDTOs;
using System.Text.RegularExpressions;

public class NotificationsService
{
    private const string TeamLeadRoleTitle = "Руководитель команды";

    private static readonly Regex MentionRegex = new(
        @"(?<!\S)@(?<name>[\p{L}\p{N}._-]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    private readonly AppDbContext _context;
    private readonly UserHelper _userHelper;
    private readonly ChatService _chat;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IConfiguration _configuration;

    public NotificationsService(
        AppDbContext context,
        UserHelper userHelper,
        ChatService chat,
        IHubContext<ChatHub> hub,
        IConfiguration configuration)
    {
        _context = context;
        _userHelper = userHelper;
        _chat = chat;
        _hub = hub;
        _configuration = configuration;
    }

    public async Task<long> CreateForMember(int memberId, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Notification text is empty.");

        text = text.Trim();

        if (text.Length > 2000)
            text = text[..2000];

        var entity = new steptreck.API.Models.Notification
        {
            MemberId = memberId,
            Text = text,
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            ReadAt = null
        };

        _context.Add(entity);
        await _context.SaveChangesAsync(ct);

        await _hub.Clients.Group($"member:{memberId}")
            .SendAsync("notify:new", new NotificationDto
            {
                Id = entity.Id,
                Text = entity.Text,
                CreatedAt = entity.CreatedAt,
                IsRead = entity.IsRead
            }, ct);

        await SendPushToMember(
            memberId,
            "Новое уведомление",
            entity.Text,
            new Dictionary<string, string>
            {
                ["type"] = "notification",
                ["notificationId"] = entity.Id.ToString()
            },
            ct
        );

        return entity.Id;
    }

    public async Task<int> CreateForTeam(int teamId, string text, CancellationToken ct = default)
    {
        var recipients = await GetRecipientIdsForLead(teamId, null, ct);

        foreach (var recipientId in recipients)
            await CreateForMember(recipientId, text, ct);

        return recipients.Count;
    }

    public async Task CreateForTeamMember(int teamId, int memberId, string text, CancellationToken ct = default)
    {
        var recipients = await GetRecipientIdsForLead(teamId, memberId, ct);
        if (recipients.Count == 0)
            throw new InvalidOperationException("Участник не найден в этой команде.");

        await CreateForMember(recipients[0], text, ct);
    }

    public async Task NotifyChatMentionsAsync(
        int teamId,
        int senderMemberId,
        string senderName,
        string text,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var mentionKeys = ExtractMentionKeys(text);
        if (mentionKeys.Count == 0)
            return;

        var team = await _context.ProjectTeams
            .AsNoTracking()
            .Where(x => x.Id == teamId && x.IsActive && !x.Project.IsArchived)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(ct);

        if (team is null)
            return;

        var members = await _context.ProjectTeamMembers
            .AsNoTracking()
            .Where(x =>
                x.TeamId == teamId &&
                x.Team.IsActive &&
                !x.Team.Project.IsArchived)
            .Select(x => new
            {
                x.MemberId,
                Username = x.Member.Username,
            })
            .ToListAsync(ct);

        var recipientIds = new HashSet<int>();
        var mentionAll = mentionKeys.Contains("all");

        if (mentionAll)
        {
            foreach (var member in members)
            {
                if (member.MemberId != senderMemberId)
                    recipientIds.Add(member.MemberId);
            }
        }

        foreach (var member in members)
        {
            if (member.MemberId == senderMemberId || string.IsNullOrWhiteSpace(member.Username))
                continue;

            if (mentionKeys.Contains(member.Username.Trim()))
                recipientIds.Add(member.MemberId);
        }

        if (recipientIds.Count == 0)
            return;

        var safeSenderName = string.IsNullOrWhiteSpace(senderName)
            ? "Участник команды"
            : senderName.Trim();

        var notificationText = BuildChatMentionText(
            safeSenderName,
            team.Name,
            text,
            mentionAll
        );

        foreach (var recipientId in recipientIds)
            await CreateForMember(recipientId, notificationText, ct);
    }

    public async Task<List<NotificationDto>> GetMyLatest(int take = 30, CancellationToken ct = default)
    {
        var myMemberId = await _chat.GetCurrentMemberId(ct);
        take = Math.Clamp(take, 1, 100);

        return await _context.Set<steptreck.API.Models.Notification>()
            .AsNoTracking()
            .Where(x => x.MemberId == myMemberId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Text = x.Text,
                CreatedAt = x.CreatedAt,
                IsRead = x.IsRead
            })
            .ToListAsync(ct);
    }

    public async Task<int> GetMyUnreadCount(CancellationToken ct = default)
    {
        var myMemberId = await _chat.GetCurrentMemberId(ct);
        return await _context.Set<steptreck.API.Models.Notification>()
            .AsNoTracking()
            .CountAsync(x => x.MemberId == myMemberId && !x.IsRead, ct);
    }

    public async Task MarkRead(long id, CancellationToken ct = default)
    {
        var myMemberId = await _chat.GetCurrentMemberId(ct);

        var n = await _context.Set<steptreck.API.Models.Notification>()
            .FirstOrDefaultAsync(x => x.Id == id && x.MemberId == myMemberId, ct);

        if (n == null) return;

        if (!n.IsRead)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task<List<int>> GetRecipientIdsForLead(
        int teamId,
        int? targetMemberId,
        CancellationToken ct)
    {
        var organizationId = _userHelper.GetCurrentOrganizationId();
        var senderMemberId = await _chat.GetCurrentMemberId(ct);

        var isLead = await _context.ProjectTeamMembers
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeamId == teamId &&
                x.MemberId == senderMemberId &&
                x.TeamRole == TeamLeadRoleTitle &&
                x.Team.IsActive &&
                !x.Team.Project.IsArchived &&
                x.Team.Project.OrganizationId == organizationId,
                ct);

        if (!isLead)
            throw new InvalidOperationException("Только руководитель команды может отправлять уведомления.");

        if (targetMemberId.HasValue && targetMemberId.Value == senderMemberId)
            throw new InvalidOperationException("Нельзя отправить личное уведомление самому себе.");

        var query = _context.ProjectTeamMembers
            .AsNoTracking()
            .Where(x =>
                x.TeamId == teamId &&
                x.Team.IsActive &&
                !x.Team.Project.IsArchived &&
                x.Team.Project.OrganizationId == organizationId);

        if (targetMemberId.HasValue)
            query = query.Where(x => x.MemberId == targetMemberId.Value);

        return await query
            .Select(x => x.MemberId)
            .Where(x => x != senderMemberId)
            .Distinct()
            .ToListAsync(ct);
    }

    private static HashSet<string> ExtractMentionKeys(string text)
    {
        return MentionRegex
            .Matches(text)
            .Select(x => x.Groups["name"].Value.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildChatMentionText(
        string senderName,
        string teamName,
        string messageText,
        bool mentionAll)
    {
        var prefix = mentionAll
            ? $"{senderName} отметил(а) @all в чате \"{teamName}\""
            : $"{senderName} упомянул(а) вас в чате \"{teamName}\"";

        var preview = messageText.Trim();
        if (preview.Length > 180)
            preview = preview[..180] + "...";

        return string.IsNullOrWhiteSpace(preview)
            ? prefix + "."
            : $"{prefix}: {preview}";
    }

    private async Task SendPushToMember(
        int memberId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        var tokens = await _context.Set<PushToken>()
            .AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .Select(x => x.Token)
            .ToListAsync(ct);

        if (tokens.Count == 0)
            return;

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body
            },
            Data = data ?? new Dictionary<string, string>()
        };

        var response = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(message, ct);

        var invalidTokens = new List<string>();

        for (var i = 0; i < response.Responses.Count; i++)
        {
            var result = response.Responses[i];

            if (!result.IsSuccess)
            {
                var errorCode = result.Exception?.MessagingErrorCode;

                if (errorCode == MessagingErrorCode.Unregistered ||
                    errorCode == MessagingErrorCode.InvalidArgument)
                {
                    invalidTokens.Add(tokens[i]);
                }
            }
        }

        if (invalidTokens.Count > 0)
        {
            await _context.Set<PushToken>()
                .Where(x => invalidTokens.Contains(x.Token))
                .ExecuteDeleteAsync(ct);
        }
    }
    public async Task<long> CreateForUser(
        int userId,
        string text,
        CancellationToken ct = default)
    {
        var memberId = await _context.Members
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (memberId == 0)
            throw new InvalidOperationException("Member для пользователя не найден.");

        return await CreateForMember(memberId, text, ct);
    }
    public async Task SendLoginChallengePush(LoginChallenge model, CancellationToken ct)
    {
        var memberId = await _context.Members
            .AsNoTracking()
            .Where(x => x.UserId == model.UserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (memberId == 0)
            throw new InvalidOperationException("Member для пользователя не найден.");

        await SendPushToMember(
            memberId,
            "Подтверждение входа",
            "Новая попытка входа в аккаунт",
            new Dictionary<string, string>
            {
                ["type"] = "login_challenge",
                ["challengeId"] = model.Id.ToString(),
                ["ip"] = model.IpAddress?.ToString() ?? "",
                ["userAgent"] = model.UserAgent ?? ""
            },
            ct
        );
    }
}
