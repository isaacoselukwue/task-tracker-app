using Microsoft.EntityFrameworkCore;
using InternalException = TaskTracker.Application.Common.Exceptions;

namespace TaskTracker.Api.Filters;

public partial class GlobalExceptionHandlingMiddleware(RequestDelegate next, Serilog.ILogger logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Npgsql.PostgresException p)
        {
            logger.Error(p, "Error: {Message}", p.Message);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(Result.Failure("An error occurred", ["We could not handle your request at this time"]));
        }
        catch (DbUpdateException p)
        {
            logger.Error(p, "Error: {Message}", p.Message);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(Result.Failure("An error occurred", ["We could not handle your request at this time"]));
        }
        catch (InternalException.ValidationException vex)
        {
            logger.Error(vex, "Validation error: {Message}", vex.Message);
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(Result.Failure("Validation failed", [.. vex.Errors.SelectMany(x => x.Value)]));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(Result.Failure("An error occurred", [e.Message]));
        }
    }
}