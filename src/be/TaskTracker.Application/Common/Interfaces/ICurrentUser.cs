namespace TaskTracker.Application.Common.Interfaces;
public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
