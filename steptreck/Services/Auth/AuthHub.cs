using Microsoft.AspNetCore.SignalR;

public class AuthHub : Hub
{
    public async Task JoinLoginChallenge(Guid challengeId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"login-challenge:{challengeId}"
        );
    }

    public async Task LeaveLoginChallenge(Guid challengeId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            $"login-challenge:{challengeId}"
        );
    }
}