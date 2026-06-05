using Microsoft.EntityFrameworkCore;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.Domain.DTOs;

public class PushTokenService
{
    private readonly AppDbContext _context;
    private readonly UserHelper _userHelper;

    public PushTokenService(AppDbContext context, UserHelper userHelper)
    {
        _context = context;
        _userHelper = userHelper;
    }

    public async Task RegisterAsync(
        RegisterPushTokenRequest request,
        CancellationToken ct
    )
    {
        var token = request.Token.Trim();

        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("Push token is empty");

        var userId = _userHelper.GetCurrentUserId();

        var member = await _context.Members
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (member == null)
            throw new Exception("Участник не найден");

        var now = DateTime.UtcNow;

        var existing = await _context.PushTokens
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        if (existing == null)
        {
            _context.PushTokens.Add(new PushToken
            {
                MemberId = member.Id,
                Token = token,
                Platform = request.Platform,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.MemberId = member.Id;
            existing.Platform = request.Platform;
            existing.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(ct);
    }
}