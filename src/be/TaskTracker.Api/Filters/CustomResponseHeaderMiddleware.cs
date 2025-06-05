using System.Diagnostics;

namespace TaskTracker.Api.Filters;

public class CustomResponseHeaderMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;

    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(state =>
        {
            var httpContext = (HttpContext)state;
            httpContext.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            httpContext.Response.Headers.Append("X-Xss-Protection", "1; mode=block");
            httpContext.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
            httpContext.Response.Headers.Append("X-Content-Security-Policy", "default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';");
            httpContext.Response.Headers.Append("Referrer-Policy", "no-referrer");
            httpContext.Response.Headers.Append("RequestId", Activity.Current?.TraceId.ToString());
            httpContext.Response.Headers.Remove("X-Powered-By");
            httpContext.Response.Headers.Remove("Server");
            httpContext.Response.Headers.Remove("server");
            httpContext.Response.Headers.Remove("x-aspnet-version");
            return Task.CompletedTask;
        }, context);
        await next(context);
    }
}