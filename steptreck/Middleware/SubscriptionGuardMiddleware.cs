using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using steptreck.API.Gate;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Middleware;
using System.Text.Json;

public class SubscriptionGuardMiddleware : IMiddleware
{
    private readonly UserHelper _userHelper;
    private readonly ISubscriptionGate _gate;

    public SubscriptionGuardMiddleware(UserHelper userHelper, ISubscriptionGate gate)
    {
        _userHelper = userHelper;
        _gate = gate;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await next(context);
            return;
        }

        if (endpoint.Metadata.GetMetadata<SkipSubscriptionCheckAttribute>() != null)
        {
            await next(context);
            return;
        }
        var hasAuthorize = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()?.Any() == true;
        if (!hasAuthorize)
        {
            await next(context);
            return;
        }

        int orgId;
        try
        {
            orgId = _userHelper.GetCurrentOrganizationId();
        }
        catch
        {
            await next(context);
            return;
        }
        if (!context.Items.TryGetValue("has_active_sub", out var cached))
        {
            var ok = await _gate.HasActiveAsync(orgId, context.RequestAborted);
            context.Items["has_active_sub"] = ok;
            cached = ok;
        }

        if (cached is bool okActive && okActive)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = JsonSerializer.Serialize(new { error = "Нет активной подписки." });
        await context.Response.WriteAsync(payload);
    }
}
