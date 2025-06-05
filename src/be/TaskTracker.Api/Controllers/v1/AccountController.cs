global using MediatR;
global using Microsoft.AspNetCore.Authorization;
global using TaskTracker.Application.Accounts.Commands;
global using TaskTracker.Application.Accounts.Queries;

namespace TaskTracker.Api.Controllers.v1;
/// <summary>
/// Account controller for managing user accounts. Here users can change password, deactivate account. 
/// Admins can activate account, view users, change user role and delete account.
/// </summary>
[ApiController]
[Authorize]
public class AccountController(ISender sender) : BaseController
{
    /// <summary>
    /// Changes the password for the currently authenticated user.
    /// </summary>
    /// <param name="command">The command containing the new password and confirmation.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the password change was successful.
    /// If successful, a confirmation email is sent to the user.
    /// </returns>
    /// <response code="200">Password change successful.</response>
    /// <response code="400">Password change failed due to validation errors or authentication issues.</response>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Deactivates the account of the currently authenticated user.
    /// </summary>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the account deactivation was successful.
    /// If successful, a notification email is sent to the user.
    /// </returns>
    /// <response code="200">Account deactivation successful.</response>
    /// <response code="400">Account deactivation failed due to authentication issues or other errors.</response>
    [HttpDelete("deactivate-account")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> DeactivateAccount()
    {
        var result = await sender.Send(new DeactivateAccountCommand());
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Activates a user account. This endpoint is accessible only by administrators.
    /// </summary>
    /// <param name="command">The command containing the user ID of the account to activate.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the account activation was successful.
    /// If successful, a notification email is sent to the user.
    /// </returns>
    /// <response code="200">Account activation successful.</response>
    /// <response code="400">Account activation failed due to invalid user ID or other errors.</response>
    /// <response code="403">Forbidden, user is not an administrator.</response>
    [Authorize(Policy = "AdminPolicy")]
    [HttpPost("admin/activate-account")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 403)]
    public async ValueTask<ActionResult<Result>> ActivateAccount([FromBody] ActivateAccountCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Retrieves paginated list of user accounts. This endpoint is accessible only by administrators.
    /// </summary>
    /// <param name="query">The query parameters specifying page number and count.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result{UserAccountDto}"/> with paginated user account data.
    /// </returns>
    /// <response code="200">User account data retrieved successfully.</response>
    /// <response code="400">Request failed due to invalid pagination parameters.</response>
    /// <response code="403">Forbidden, user is not an administrator.</response>
    [Authorize(Policy = "AdminPolicy")]
    [HttpGet("admin/users")]
    [ProducesResponseType(typeof(Result<UserAccountDto>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 403)]
    public async ValueTask<ActionResult<Result<UserAccountDto>>> ViewUsers([FromQuery] UserAccountQuery query)
    {
        var result = await sender.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Changes the role of a user. This endpoint is accessible only by administrators.
    /// </summary>
    /// <param name="command">The command containing the user ID and new role.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the role change was successful.
    /// If successful, a notification email is sent to the user.
    /// </returns>
    /// <response code="200">Role change successful.</response>
    /// <response code="400">Role change failed due to invalid user ID, invalid role, or other errors.</response>
    /// <response code="403">Forbidden, user is not an administrator.</response>
    [Authorize(Policy = "AdminPolicy")]
    [HttpPost("admin/change-role")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 403)]
    public async ValueTask<ActionResult<Result>> ChangeRole([FromBody] ChangeUserRoleCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Deactivates a user account by an administrator. This endpoint is accessible only by administrators.
    /// </summary>
    /// <param name="command">The command containing the user ID of the account to deactivate.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the account deactivation was successful.
    /// If successful, a notification email is sent to the user.
    /// </returns>
    /// <response code="200">Account deactivation successful.</response>
    /// <response code="400">Account deactivation failed due to invalid user ID or other errors.</response>
    /// <response code="403">Forbidden, user is not an administrator.</response>
    [Authorize(Policy = "AdminPolicy")]
    [HttpDelete("admin/deactivate-account")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 403)]
    public async ValueTask<ActionResult<Result>> DeactivateAccount([FromBody] DeactivateAccountAdminCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Deletes a user account. This endpoint is accessible only by administrators.
    /// </summary>
    /// <param name="command">The command containing the user ID and whether deletion should be permanent.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the account deletion was successful.
    /// If successful, a notification email is sent to the user.
    /// </returns>
    /// <response code="200">Account deletion successful.</response>
    /// <response code="400">Account deletion failed due to invalid user ID or other errors.</response>
    /// <response code="403">Forbidden, user is not an administrator.</response>
    [Authorize(Policy = "AdminPolicy")]
    [HttpDelete("admin/delete-account")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    [ProducesResponseType(typeof(Result), 403)]
    public async ValueTask<ActionResult<Result>> DeleteAccount([FromBody] DeleteAccountCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Initiates the password reset process for a user by sending a reset token via email.
    /// </summary>
    /// <param name="command">The command containing the email address of the user requesting a password reset.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the password reset initiation was successful.
    /// If successful, a password reset email is sent to the user.
    /// </returns>
    /// <response code="200">Password reset initiation successful; an email with reset instructions has been sent.</response>
    /// <response code="400">Password reset initiation failed due to invalid email or other errors.</response>
    [AllowAnonymous]
    [HttpPost("password-reset/initial")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> PasswordResetInitial([FromBody] InitiatePasswordResetCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Completes the password reset process using the token received via email.
    /// </summary>
    /// <param name="command">The command containing the user ID, reset token, and new password information.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the password reset was successful.
    /// If successful, a confirmation email is sent to the user.
    /// </returns>
    /// <response code="200">Password reset successful.</response>
    /// <response code="400">Password reset failed due to invalid token, password mismatch, or other errors.</response>
    [AllowAnonymous]
    [HttpPost("password-reset")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> PasswordReset([FromBody] PasswordResetCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
