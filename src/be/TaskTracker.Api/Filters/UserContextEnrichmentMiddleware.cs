global using System.Diagnostics;
global using System.IdentityModel.Tokens.Jwt;

namespace TaskTracker.Api.Filters;

public sealed class UserContextEnrichmentMiddleware(RequestDelegate next, ILogger<UserContextEnrichmentMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string? userId = context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId is not null)
        {
            Activity.Current?.SetTag("user.id", userId);

            Dictionary<string, object> data = new()
            {
                ["UserId"] = userId
            };

            using (logger.BeginScope(data))
            {
                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }
}