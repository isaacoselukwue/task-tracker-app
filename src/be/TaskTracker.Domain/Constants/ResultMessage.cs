namespace TaskTracker.Domain.Constants;
public class ResultMessage
{
    public const string ActivateAccountFailed = "Account activation failed";
    public const string ActivateAccountSuccess = "Account activation succeeded";
    public const string AccessTokenGenerated = "Access token generated successfully.";
    public const string ChangePasswordFailed = "You cannot change password at this time.";
    public const string ChangePasswordSuccess = "Your password was changed successfully.";
    public const string ChangeUserRoleFailed = "Role change failed at this time";
    public const string ChangeUserRoleSuccess = "Role change successful";
    public const string DeactivateAccountFailed = "Your account cannot be deactivated at this time";
    public const string DeactivateAccountSuccess = "Your account was deactivated successfully";
    public const string DeleteAccountFailed = "Account deletion failed";
    public const string DeleteAccountSuccess = "Account deletion succeeded";
    public const string ForgotPasswordFailed = "We cannot reset password at this time.";
    public const string ForgotPasswordSuccess = "Password reset successful. Please check your mail";
    public const string LoginFailedGeneric = "Login failed. Please check your credentials.";
    public const string LoginFailedAccountLocked = "Account might be locked out. Please retry in 24 hours";
    public const string ResetPasswordFailed = "We could not reset password at this time. Please try again later";
    public const string ResetPasswordSuccess = "Password reset successful. Please reattempt login with new password.";
    public const string SignUpFailed = "Sign up failed. Please review errors and try again.";
    public const string SignUpSuccess = "Sign up successful.";
    public const string TokenRefreshFailed = "Token refresh failed.";
}

public class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}