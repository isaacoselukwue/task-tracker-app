global using TaskTracker.Application.Authentication.Commands;

namespace TaskTracker.Api.Controllers.v1;
[ApiController]
public class AuthenticationController(ISender sender) : BaseController
{
    /// <summary>
    /// Authenticates a user with the given credentials.
    /// </summary>
    /// <param name="command">The login command including email address and password.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result{LoginDto}"/> with an access token if login is successful;
    /// otherwise, a failure message.
    /// </returns>
    /// <response code="200">Login successful and access token is returned.</response>
    /// <response code="400">Login failed due to invalid credentials or account restrictions.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(Result<LoginDto>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result<LoginDto>>> Login([FromBody] LoginCommand command)
    {
        var result = await sender.Send(command);
        AddRefreshToken(result);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Refreshes an authentication token using a refresh token.
    /// </summary>
    /// <param name="command">The refresh token command containing the encrypted token.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result{LoginDto}"/> with a new access token if refresh is successful;
    /// otherwise, a failure message.
    /// </returns>
    /// <response code="200">Token refresh successful and new access token is returned.</response>
    /// <response code="400">Token refresh failed due to invalid, expired, or revoked token.</response>
    [HttpPost("login/refresh")]
    [ProducesResponseType(typeof(Result<LoginDto>), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result<LoginDto>>> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await sender.Send(command);
        AddRefreshToken(result);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// </summary>
    /// <param name="command">The revoke token command containing the encrypted token to revoke.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the logout was successful.
    /// </returns>
    /// <response code="200">Logout successful; the refresh token has been revoked.</response>
    /// <response code="400">Logout failed due to invalid token or other errors.</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> Logout([FromBody] RevokeRefreshTokenCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Registers a new user and sends an activation email.
    /// </summary>
    /// <param name="command">
    /// The signup command including email address, password, confirm password, first name, last name, and phone number.
    /// </param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the signup was successful.
    /// In case of success, an activation token is generated and an event is published to send an activation email.
    /// </returns>
    /// <response code="200">Signup successful; an activation email is sent to the user.</response>
    /// <response code="400">Signup failed due to validation errors or account duplication.</response>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> SignUp([FromBody] SignupCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Verifies a user's account using the activation token sent during signup.
    /// </summary>
    /// <param name="command">The verification command including user ID and activation token.</param>
    /// <returns>
    /// Returns an <see cref="ActionResult{T}"/> containing a <see cref="Result"/> indicating whether the verification was successful.
    /// If successful, a confirmation email is sent to the user.
    /// </returns>
    /// <response code="200">Verification successful; the user's account has been activated.</response>
    /// <response code="400">Verification failed due to invalid token, expired token, or already verified account.</response>
    [HttpPost("signup/verify")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> VerifySignup([FromBody] SignupVerificationCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    private void AddRefreshToken(Result<LoginDto> result)
    {
        if (result.Succeeded && result.Data is not null && result.Data.AccessToken is not null)
        {
            Response.Cookies.Append("refreshToken", result.Data.AccessToken.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMonths(1)
            });
        }
    }
}