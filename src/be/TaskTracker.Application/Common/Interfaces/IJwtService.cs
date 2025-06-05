global using System.Security.Claims;

namespace TaskTracker.Application.Common.Interfaces;
public interface IJwtService
{
    Result<LoginDto> GenerateToken(Users user, List<Claim> userClaims, List<string> roles);
    string GetEmailAddress();
    Guid GetUserId();
    (string token, string userId) UnprotectToken(string protectedToken);
}
