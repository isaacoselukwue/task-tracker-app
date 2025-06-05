using System.Web;
using TaskTracker.Application.Accounts.Queries;
using TaskTracker.Application.Authentication.Commands;

namespace TaskTracker.Infrastructure.Identity;
public class IdentityService(
    SignInManager<Users> signInManager,
    UserManager<Users> userManager,
    IJwtService jwtService,
    ITaskDbContext taskDbContext) : IIdentityService
{
    public async Task<(Result, string email)> ActivateAccountAsync(Guid userId)
    {
        Users? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return (Result.Failure(ResultMessage.ActivateAccountFailed, ["Invalid user"]), string.Empty);
        }
        if (user.UsersStatus == Domain.Enums.StatusEnum.Active)
            return (Result.Failure(ResultMessage.ActivateAccountFailed, ["Account is already active"]), string.Empty);
        user.UsersStatus = Domain.Enums.StatusEnum.Active;
        user.LastModified = DateTimeOffset.UtcNow;
        user.LastModifiedBy = jwtService.GetEmailAddress();
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.ActivateAccountFailed), string.Empty);
        }
        return (Result.Success(ResultMessage.ActivateAccountSuccess), user.Email!);
    }
    public async Task<(Result, string email)> ChangePasswordAsync(string newPassword)
    {
        Users? user = await userManager.FindByIdAsync(jwtService.GetUserId().ToString());
        if (user is null)
        {
            return (Result.Failure(ResultMessage.ChangePasswordFailed, ["Invalid user"]), string.Empty);
        }
        if (user.UsersStatus != Domain.Enums.StatusEnum.Active)
            return (Result.Failure(ResultMessage.ChangePasswordFailed, ["Account is not active"]), string.Empty);

        IdentityResult result = await userManager.ChangePasswordAsync(user, user.PasswordHash!, newPassword);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.ChangePasswordFailed), string.Empty);
        }
        await signInManager.RefreshSignInAsync(user);
        await LogPasswordChangeHistoryAsync(user.Id.ToString(), user.PasswordHash!);
        return (Result.Success(ResultMessage.ChangePasswordSuccess), user.Email!);
    }

    public async Task<(Result, string email)> ChangeUserRoleAsync(string userId, string role)
    {
        Users? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (Result.Failure(ResultMessage.ChangeUserRoleFailed, ["Invalid user"]), string.Empty);
        }
        if (user.UsersStatus != Domain.Enums.StatusEnum.Active)
            return (Result.Failure(ResultMessage.ChangeUserRoleFailed, ["Account is not active"]), string.Empty);
        IList<string> roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(role))
        {
            return (Result.Failure(ResultMessage.ChangeUserRoleFailed, ["User already has the role"]), user.Email!);
        }
        IdentityResult result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.ChangeUserRoleFailed), string.Empty);
        }
        return (Result.Success(ResultMessage.ChangeUserRoleSuccess), user.Email!);
    }
    public async Task<(Result, string usersEmail)> DeactivateAccountAsync()
    {
        Users? user = await userManager.FindByIdAsync(jwtService.GetUserId().ToString());
        if (user is null)
        {
            return (Result.Failure(ResultMessage.DeactivateAccountFailed, ["Invalid user"]), string.Empty);
        }
        if (user.UsersStatus != Domain.Enums.StatusEnum.Active)
            return (Result.Failure(ResultMessage.DeactivateAccountFailed, ["Account is not active"]), string.Empty);
        user.UsersStatus = Domain.Enums.StatusEnum.InActive;
        user.LastModified = DateTimeOffset.UtcNow;
        user.LastModifiedBy = jwtService.GetEmailAddress();
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.DeactivateAccountFailed), string.Empty);
        }
        return (Result.Success(ResultMessage.DeactivateAccountSuccess), user.Email!);
    }

    public async Task<(Result, string usersEmail)> DeactivateAccountAsync(Guid userId)
    {
        Users? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return (Result.Failure(ResultMessage.DeactivateAccountFailed, ["Invalid user"]), string.Empty);
        }
        if (user.UsersStatus != Domain.Enums.StatusEnum.Active)
            return (Result.Failure(ResultMessage.DeactivateAccountFailed, ["Account is not active"]), string.Empty);

        user.UsersStatus = Domain.Enums.StatusEnum.InActive;
        user.LastModified = DateTimeOffset.UtcNow;
        user.LastModifiedBy = jwtService.GetEmailAddress();

        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.DeactivateAccountFailed), string.Empty);
        }
        return (Result.Success(ResultMessage.DeactivateAccountSuccess), user.Email!);
    }

    public async Task<(Result, string usersEmail)> DeleteUserAsync(string userId, bool isPermanant)
    {
        Users? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (Result.Failure(ResultMessage.DeleteAccountFailed, ["Invalid user"]), string.Empty);
        }

        if (user.UsersStatus == Domain.Enums.StatusEnum.Deleted && !isPermanant)
            return (Result.Failure(ResultMessage.DeleteAccountFailed, ["Account is already on soft delete"]), string.Empty);

        IdentityResult result;
        if (!isPermanant)
        {
            user.UsersStatus = Domain.Enums.StatusEnum.Deleted;
            user.LastModified = DateTimeOffset.UtcNow;
            user.LastModifiedBy = jwtService.GetEmailAddress();
            result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return (result.ToApplicationResult(ResultMessage.DeleteAccountFailed), string.Empty);
            }
            return (Result.Success(ResultMessage.DeleteAccountSuccess), user.Email!);
        }

        result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.DeleteAccountFailed), string.Empty);
        }
        return (Result.Success(ResultMessage.DeleteAccountSuccess), user.Email!);
    }
    public async Task<(Result result, string emailAddress)> InitiateForgotPasswordAsync(string emailAddress)
    {
        Users? user = await userManager.FindByEmailAsync(emailAddress);
        if (user is null)
        {
            return (Result.Success(ResultMessage.ForgotPasswordSuccess), string.Empty);
        }
        if (user.UsersStatus != Domain.Enums.StatusEnum.Active)
            return (Result.Success(ResultMessage.ForgotPasswordSuccess), string.Empty);

        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(30));

        string encodedToken = HttpUtility.UrlEncode(token);
        return (Result.Success(user.Id.ToString()), encodedToken);
    }
    private async Task LogPasswordChangeHistoryAsync(string userId, string newPasswordHash)
    {
        PasswordHistories passwordHistories = new()
        {
            Created = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = userId,
            PasswordHash = newPasswordHash,
            UserId = Guid.Parse(userId)
        };
        await taskDbContext.PasswordHistories.AddAsync(passwordHistories);
        await taskDbContext.SaveChangesAsync(CancellationToken.None);
    }
    public async Task<Result<LoginDto>> RefreshUserTokenAsync(string encryptedToken)
    {
        (_, string userId) = jwtService.UnprotectToken(encryptedToken);
        Users? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<LoginDto>.Failure(ResultMessage.TokenRefreshFailed, ["Invalid user"]);
        }
        if (user.UsersStatus != Domain.Enums.StatusEnum.Active)
        {
            return Result<LoginDto>.Failure(ResultMessage.TokenRefreshFailed, ["Account is not active"]);
        }

        string? validToken = await userManager.GetAuthenticationTokenAsync(user, "MediaLocator", "RefreshToken");
        if (validToken != encryptedToken)
        {
            return Result<LoginDto>.Failure(ResultMessage.TokenRefreshFailed, ["Invalid token"]);
        }

        List<Claim> claims = (List<Claim>)await userManager.GetClaimsAsync(user);
        List<string> roles = (List<string>)await userManager.GetRolesAsync(user);

        var tokenResult = jwtService.GenerateToken(user, claims, roles);
        if (tokenResult.Succeeded)
        {
            await userManager.RemoveAuthenticationTokenAsync(user, "MediaLocator", "RefreshToken");
            await userManager.SetAuthenticationTokenAsync(user, "MediaLocator", "RefreshToken", tokenResult.Data!.AccessToken!.RefreshToken);
        }

        return tokenResult;
    }

    public async Task<(Result result, string emailAddress)> ResetPasswordAsync(string newPassword, string userId, string passwordResetToken)
    {
        Users? users = await userManager.FindByIdAsync(userId);
        if (users is null)
        {
            return (Result.Failure(ResultMessage.ResetPasswordFailed, ["Invalid user"]), string.Empty);
        }

        IdentityResult result = await userManager.ResetPasswordAsync(users, passwordResetToken, newPassword);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.ResetPasswordFailed), string.Empty);
        }

        await userManager.SetLockoutEndDateAsync(users, DateTimeOffset.UtcNow);
        return (Result.Success(ResultMessage.ResetPasswordSuccess), users.Email!);
    }

    public async Task<Result> RevokeRefreshUserTokenAsync(string encryptedToken)
    {
        (_, string userId) = jwtService.UnprotectToken(encryptedToken);
        Users? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result.Failure(ResultMessage.TokenRefreshFailed, ["Invalid user"]);
        }
        //if (user.UsersStatus != Domain.Enums.StatusEnum.Active)
        //{
        //    return Result.Failure(ResultMessage.TokenRefreshFailed, ["Account is not active"]);
        //}

        string? validToken = await userManager.GetAuthenticationTokenAsync(user, "MediaLocator", "RefreshToken");
        if (validToken != encryptedToken)
        {
            return Result.Failure(ResultMessage.TokenRefreshFailed, ["Invalid token"]);
        }

        await userManager.RemoveAuthenticationTokenAsync(user, "MediaLocator", "RefreshToken");

        return Result.Success("Refresh token successfully revoked");
    }
    public async Task<Result<LoginDto>> SignInUserAsync(string username, string password)
    {
        var result = await signInManager.PasswordSignInAsync(username, password, false, true);
        if (result.IsLockedOut)
        {
            return Result<LoginDto>.Failure(ResultMessage.LoginFailedGeneric, [ResultMessage.LoginFailedAccountLocked]);
        }
        else if (result.IsNotAllowed)
        {
            return Result<LoginDto>.Failure(ResultMessage.LoginFailedGeneric, ["Please complete account sign up"]);
        }
        else if (!result.Succeeded)
        {
            return Result<LoginDto>.Failure(ResultMessage.LoginFailedGeneric, ["Invalid username or password"]);
        }

        Users? user = await userManager.FindByEmailAsync(username);
        if (user is null)
            return Result<LoginDto>.Failure(ResultMessage.LoginFailedGeneric, ["Invalid username or password"]);

        if (user.UsersStatus != Domain.Enums.StatusEnum.Active)
            return Result<LoginDto>.Failure(ResultMessage.LoginFailedGeneric, ["Account is not active"]);

        List<Claim> claims = (List<Claim>)await userManager.GetClaimsAsync(user);
        List<string> roles = (List<string>)await userManager.GetRolesAsync(user);

        var token = jwtService.GenerateToken(user, claims, roles);
        if (token.Succeeded)
            await userManager.SetAuthenticationTokenAsync(user, "MediaLocator", "RefreshToken", token.Data!.AccessToken!.RefreshToken);

        return token;
    }
    public async Task<(Result, string token)> SignUpUserAsync(string email, string password, string firstName, string lastName, string phoneNumber)
    {
        Users user = new()
        {
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            EmailConfirmed = false,
            Created = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
            CreatedBy = email,
            LastModifiedBy = email
        };

        IdentityResult result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.SignUpFailed), string.Empty);
        }
        IdentityResult roleResult = await userManager.AddToRoleAsync(user, Roles.User);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return (roleResult.ToApplicationResult(ResultMessage.SignUpFailed), string.Empty);
        }

        IdentityResult claimResult = await userManager.AddClaimAsync(user, new Claim("Permission", "CanView"));
        if (!claimResult.Succeeded)
        {
            await userManager.RemoveFromRoleAsync(user, Roles.User);
            await userManager.DeleteAsync(user);
            return (claimResult.ToApplicationResult(ResultMessage.SignUpFailed), string.Empty);
        }

        await LogPasswordChangeHistoryAsync(user.Id.ToString(), user.PasswordHash!);

        string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        string encodedToken = HttpUtility.UrlEncode(token);
        return (result.ToApplicationResult(user.Id.ToString()), encodedToken);
    }
    public IQueryable<Users> UserAccounts() => userManager.Users;
    public IQueryable<UserAccountResult> UserAccountWithRoles()
    {
        var userAccounts = from user in taskDbContext.Users
                           join userRole in taskDbContext.UserRoles on user.Id equals userRole.UserId
                           join role in taskDbContext.Roles on userRole.RoleId equals role.Id
                           group role by user into userGroup
                           select new UserAccountResult
                           {
                               UserId = userGroup.Key.Id,
                               FirstName = userGroup.Key.FirstName,
                               LastName = userGroup.Key.LastName,
                               EmailAddress = userGroup.Key.Email,
                               PhoneNumber = userGroup.Key.PhoneNumber,
                               DateAccountCreated = userGroup.Key.Created,
                               Status = userGroup.Key.UsersStatus,
                               Roles = userGroup.Select(r => r.Name!).ToList()
                           };

        return userAccounts;
    }


    public async Task<(Result, string usersEmail)> ValidateSignupAsync(string userId, string activationToken)
    {
        Users? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (Result.Failure(ResultMessage.SignUpFailed, ["Invalid user"]), string.Empty);
        }
        if (user.UsersStatus != Domain.Enums.StatusEnum.Pending)
        {
            return (Result.Failure(ResultMessage.SignUpFailed, ["Invalid user"]), string.Empty);
        }
        if (user.EmailConfirmed)
        {
            return (Result.Failure(ResultMessage.SignUpFailed, ["User already activated"]), string.Empty);
        }
        IdentityResult result = await userManager.ConfirmEmailAsync(user, activationToken);
        if (!result.Succeeded)
        {
            return (result.ToApplicationResult(ResultMessage.SignUpFailed), string.Empty);
        }
        user.UsersStatus = Domain.Enums.StatusEnum.Active;
        user.LastModified = DateTimeOffset.UtcNow;
        user.LastModifiedBy = user.Email;
        await userManager.UpdateAsync(user);
        return (Result.Success(ResultMessage.SignUpSuccess), user.Email!);
    }
}