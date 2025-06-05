global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Filters;
global using TaskTracker.Application.Common.Models;

namespace TaskTracker.Api.Filters;

public class ApiKeyAuthorizationFilter(IConfiguration configuration) : IAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration = configuration;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!IsApiKeyValid(context.HttpContext))
        {
            context.Result = new UnauthorizedObjectResult(Result.Failure("Unauthorized user", ["Invalid API key"]));
        }
    }

    private bool IsApiKeyValid(HttpContext context)
    {
        string? apiKey = context.Request.Headers[ApiKeyHeaderName];
        if (string.IsNullOrWhiteSpace(apiKey)) return false;

        string actualApiKey = _configuration.GetValue<string>("ApiKey")!;
        return apiKey == actualApiKey;
    }
}