namespace TaskTracker.Domain.Enums;
public enum NotificationTypeEnum
{
    SignUpAccountActivation = 1,
    SignUpCompleted,
    SignUpFailure,
    SignInSuccess,
    SignInBlockedAccount,
    DeleteAccountSuccess,
    DeactivateAccountSuccess,
    ChangeRoleSuccess,
    ChangePasswordSuccess,
    AccountActivationAdmin,
    PasswordResetInitiation,
    PasswordResetSuccess,
    UpcomingTaskReminder
}
