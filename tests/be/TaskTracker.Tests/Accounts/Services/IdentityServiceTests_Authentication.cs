namespace TaskTracker.Tests.Accounts.Services;

class IdentityServiceTests_Authentication : IdentityServiceTests
{
    private string _token;
    private string _refreshToken;
    private LoginDto _loginDto;
    
    [SetUp]
    public new void Setup()
    {
        base.Setup();
        
        _token = "test-token";
        _refreshToken = "refresh-token";
        
        _loginDto = new LoginDto 
        {
            AccessToken = new Microsoft.AspNetCore.Authentication.BearerToken.AccessTokenResponse 
            { 
                AccessToken = _token,
                RefreshToken = _refreshToken,
                ExpiresIn = DateTimeOffset.UtcNow.AddHours(1).Ticks
            }
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(_validUser.Email!)).ReturnsAsync(_validUser);
            
        _mockUserManager.Setup(x => x.FindByEmailAsync("invalid@example.com")).ReturnsAsync((Users?)null);
            
        _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(_validUser)).ReturnsAsync(_token);
            
        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(_validUser, It.IsAny<DateTimeOffset>())).ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.ResetPasswordAsync(_validUser, _token, _newPassword)).ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.ResetPasswordAsync(_updateFailUser, _token, _newPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Reset failed" }));
            
        _mockJwtService.Setup(x => x.UnprotectToken(_refreshToken)).Returns((_token, _validUserId.ToString()));
            
        _mockJwtService.Setup(x => x.GenerateToken(_validUser, It.IsAny<List<Claim>>(), It.IsAny<List<string>>()))
            .Returns(Result<LoginDto>.Success("Token generated successfully", _loginDto));
            
        _mockJwtService.Setup(x => x.GenerateToken(_updateFailUser, It.IsAny<List<Claim>>(), It.IsAny<List<string>>()))
            .Returns(Result<LoginDto>.Failure("Token generation failed", ["Failed to generate token"]));
            
        _mockUserManager.Setup(x => x.GetAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken")).ReturnsAsync(_refreshToken);
            
        _mockUserManager.Setup(x => x.RemoveAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken")).ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.SetAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken", It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.GetClaimsAsync(_validUser)).ReturnsAsync([new("Permission", "CanView")]);
            
        _mockUserManager.Setup(x => x.GetRolesAsync(_validUser)).ReturnsAsync(["User"]);
            
        _mockSignInManager.Setup(x => x.PasswordSignInAsync(_validUser.Email!, _newPassword, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            
        _mockSignInManager.Setup(x => x.PasswordSignInAsync("locked@example.com", _newPassword, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
            
        _mockSignInManager.Setup(x => x.PasswordSignInAsync("notallowed@example.com", _newPassword, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.NotAllowed);
            
        _mockSignInManager.Setup(x => x.PasswordSignInAsync("invalid@example.com", _newPassword, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
    }

    [Test]
    public async Task InitiateForgotPassword_UserNotFound_ReturnsSuccessWithEmptyToken()
    {
        var result = await _identityService.InitiateForgotPasswordAsync("invalid@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.result.Succeeded, Is.True);
            Assert.That(result.result.Message, Is.EqualTo(ResultMessage.ForgotPasswordSuccess));
            Assert.That(result.result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task InitiateForgotPassword_InactiveUser_ReturnsSuccessWithEmptyToken()
    {
        _validUser.UsersStatus = StatusEnum.InActive;
        
        var result = await _identityService.InitiateForgotPasswordAsync(_validUser.Email!);

        Assert.Multiple(() =>
        {
            Assert.That(result.result.Succeeded, Is.True);
            Assert.That(result.result.Message, Is.EqualTo(ResultMessage.ForgotPasswordSuccess));
            Assert.That(result.result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task InitiateForgotPassword_ActiveUser_ReturnsSuccessWithToken()
    {
        _validUser.UsersStatus = StatusEnum.Active;
        
        var result = await _identityService.InitiateForgotPasswordAsync(_validUser.Email!);

        Assert.Multiple(() =>
        {
            Assert.That(result.result.Succeeded, Is.True);
            Assert.That(result.result.Message, Is.EqualTo(_validUserId.ToString()));
            Assert.That(result.result.Errors, Is.Empty);
        });
        
        _mockUserManager.Verify(x => x.GeneratePasswordResetTokenAsync(_validUser), Times.Once);
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(_validUser, It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Test]
    public async Task ResetPassword_UserNotFound_ReturnsFailed()
    {
        var result = await _identityService.ResetPasswordAsync(_newPassword, _invalidUserId.ToString(), _token);

        Assert.Multiple(() =>
        {
            Assert.That(result.result.Succeeded, Is.False);
            Assert.That(result.result.Errors, Contains.Item("Invalid user"));
            Assert.That(result.emailAddress, Is.Empty);
        });
    }

    [Test]
    public async Task ResetPassword_ResetFails_ReturnsFailed()
    {
        var result = await _identityService.ResetPasswordAsync(_newPassword, _updateFailUserId.ToString(), _token);

        Assert.Multiple(() =>
        {
            Assert.That(result.result.Succeeded, Is.False);
            Assert.That(result.result.Errors, Contains.Item("Reset failed"));
            Assert.That(result.emailAddress, Is.Empty);
        });
    }

    [Test]
    public async Task ResetPassword_Success_ReturnsSuccessWithEmail()
    {
        var result = await _identityService.ResetPasswordAsync(_newPassword, _validUserId.ToString(), _token);

        Assert.Multiple(() =>
        {
            Assert.That(result.result.Succeeded, Is.True);
            Assert.That(result.result.Message, Is.EqualTo(ResultMessage.ResetPasswordSuccess));
            Assert.That(result.emailAddress, Is.EqualTo(_validUser.Email));
        });
        
        _mockUserManager.Verify(x => x.ResetPasswordAsync(_validUser, _token, _newPassword), Times.Once);
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(_validUser, It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Test]
    public async Task RefreshUserToken_UserNotFound_ReturnsFailed()
    {
        _mockJwtService.Setup(x => x.UnprotectToken(_refreshToken))
            .Returns((_token, _invalidUserId.ToString()));
            
        var result = await _identityService.RefreshUserTokenAsync(_refreshToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });
    }

    [Test]
    public async Task RefreshUserToken_InactiveUser_ReturnsFailed()
    {
        _validUser.UsersStatus = StatusEnum.InActive;
        
        var result = await _identityService.RefreshUserTokenAsync(_refreshToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Account is not active"));
        });
    }

    [Test]
    public async Task RefreshUserToken_InvalidToken_ReturnsFailed()
    {
        _validUser.UsersStatus = StatusEnum.Active;
        _mockUserManager.Setup(x => x.GetAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken"))
            .ReturnsAsync("different-token");
            
        var result = await _identityService.RefreshUserTokenAsync(_refreshToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid token"));
        });
    }

    [Test]
    public async Task RefreshUserToken_Success_ReturnsToken()
    {
        _validUser.UsersStatus = StatusEnum.Active;
        
        var result = await _identityService.RefreshUserTokenAsync(_refreshToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data?.AccessToken, Is.Not.Null);
            Assert.That(result.Data?.AccessToken?.AccessToken, Is.Not.Null);
        });
        
        _mockUserManager.Verify(x => x.RemoveAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken"), Times.Once);
        _mockUserManager.Verify(x => x.SetAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task RevokeRefreshUserToken_UserNotFound_ReturnsFailed()
    {
        _mockJwtService.Setup(x => x.UnprotectToken(_refreshToken))
            .Returns((_token, _invalidUserId.ToString()));
            
        var result = await _identityService.RevokeRefreshUserTokenAsync(_refreshToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });
    }

    [Test]
    public async Task RevokeRefreshUserToken_InvalidToken_ReturnsFailed()
    {
        _mockUserManager.Setup(x => x.GetAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken"))
            .ReturnsAsync("different-token");
            
        var result = await _identityService.RevokeRefreshUserTokenAsync(_refreshToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid token"));
        });
    }

    [Test]
    public async Task RevokeRefreshUserToken_Success_ReturnsSuccess()
    {
        var result = await _identityService.RevokeRefreshUserTokenAsync(_refreshToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("Refresh token successfully revoked"));
        });
        
        _mockUserManager.Verify(x => x.RemoveAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken"), Times.Once);
    }

    [Test]
    public async Task SignInUser_UserLockedOut_ReturnsFailed()
    {
        var result = await _identityService.SignInUserAsync("locked@example.com", _newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item(ResultMessage.LoginFailedAccountLocked));
        });
    }

    [Test]
    public async Task SignInUser_UserNotAllowed_ReturnsFailed()
    {
        var result = await _identityService.SignInUserAsync("notallowed@example.com", _newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Please complete account sign up"));
        });
    }

    [Test]
    public async Task SignInUser_InvalidCredentials_ReturnsFailed()
    {
        var result = await _identityService.SignInUserAsync("invalid@example.com", _newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid username or password"));
        });
    }

    [Test]
    public async Task SignInUser_UserNotFound_ReturnsFailed()
    {
        _mockUserManager.Setup(x => x.FindByEmailAsync(_validUser.Email!))
            .ReturnsAsync((Users?)null);
            
        var result = await _identityService.SignInUserAsync(_validUser.Email!, _newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid username or password"));
        });
    }

    [Test]
    public async Task SignInUser_InactiveUser_ReturnsFailed()
    {
        _validUser.UsersStatus = StatusEnum.InActive;
        
        var result = await _identityService.SignInUserAsync(_validUser.Email!, _newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Account is not active"));
        });
    }

    [Test]
    public async Task SignInUser_Success_ReturnsToken()
    {
        _validUser.UsersStatus = StatusEnum.Active;
        
        var result = await _identityService.SignInUserAsync(_validUser.Email!, _newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data?.AccessToken, Is.Not.Null);
            Assert.That(result.Data?.AccessToken?.AccessToken, Is.Not.Null);
        });
        
        _mockUserManager.Verify(x => x.SetAuthenticationTokenAsync(_validUser, "MediaLocator", "RefreshToken", It.IsAny<string>()), Times.Once);
    }
}