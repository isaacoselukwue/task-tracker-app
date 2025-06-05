global using TaskTracker.Application.Common.Models;

namespace TaskTracker.Infrastructure.Identity;
public static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result, string message)
    {
        return result.Succeeded
            ? Result.Success(message)
            : Result.Failure(message, result.Errors.Select(e => e.Description));
    }
}