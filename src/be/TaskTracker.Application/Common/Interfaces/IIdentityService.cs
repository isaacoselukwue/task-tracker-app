global using TaskTracker.Application.Accounts.Queries;
global using TaskTracker.Application.Authentication.Commands;
global using TaskTracker.Application.Common.Models;
global using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Common.Interfaces;
public interface IIdentityService
{
    Task<(Result, string email)> ActivateAccountAsync(Guid userId);
    Task<(Result, string email)> ChangePasswordAsync(string newPassword);
    Task<(Result, string email)> ChangeUserRoleAsync(string userId, string role);
    Task<(Result, string usersEmail)> DeactivateAccountAsync();
    Task<(Result, string usersEmail)> DeactivateAccountAsync(Guid userId);
    Task<(Result, string usersEmail)> DeleteUserAsync(string userId, bool isPermanant);
    Task<(Result result, string emailAddress)> InitiateForgotPasswordAsync(string emailAddress);
    Task<Result<LoginDto>> RefreshUserTokenAsync(string encryptedToken);
    Task<(Result result, string emailAddress)> ResetPasswordAsync(string newPassword, string userId, string passwordResetToken);
    Task<Result> RevokeRefreshUserTokenAsync(string encryptedToken);
    Task<Result<LoginDto>> SignInUserAsync(string username, string password);
    Task<(Result, string token)> SignUpUserAsync(string email, string password, string firstName, string lastName, string phoneNumber);
    IQueryable<Users> UserAccounts();
    IQueryable<UserAccountResult> UserAccountWithRoles();
    Task<(Result, string usersEmail)> ValidateSignupAsync(string userId, string activationToken);
}
