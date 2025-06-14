namespace TaskTracker.Tests.Accounts.Services;

class IdentityServiceTests_Registration : IdentityServiceTests
{
    private string _newEmail;
    private string _firstName;
    private string _lastName;
    private string _phoneNumber;
    private string _activationToken;
    
    [SetUp]
    public new void Setup()
    {
        base.Setup();
        
        _newEmail = "newuser@example.com";
        _firstName = "New";
        _lastName = "User";
        _phoneNumber = "1234567890";
        _activationToken = "activation-token";
        
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Users>(), _newPassword))
            .ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<Users>(), Roles.User))
            .ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.AddClaimAsync(It.IsAny<Users>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<Users>()))
            .ReturnsAsync(_activationToken);
            
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(_validUser, _activationToken))
            .ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(_updateFailUser, _activationToken))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Confirmation failed" }));
            
        _validUser.UsersStatus = StatusEnum.Pending;
        _validUser.EmailConfirmed = false;
        
        _alreadyActiveUser.UsersStatus = StatusEnum.Active;
        _alreadyActiveUser.EmailConfirmed = true;
    }

    [Test]
    public async Task SignUpUser_Success_ReturnsTokenAndUserId()
    {
        var result = await _identityService.SignUpUserAsync(_newEmail, _newPassword, _firstName, _lastName, _phoneNumber);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.True);
            Assert.That(result.token, Is.EqualTo(_activationToken));
        });
        
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<Users>(), _newPassword), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<Users>(), Roles.User), Times.Once);
        _mockUserManager.Verify(x => x.AddClaimAsync(It.IsAny<Users>(), It.Is<Claim>(c => c.Type == "Permission" && c.Value == "CanView")), Times.Once);
        _mockUserManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<Users>()), Times.Once);
    }

    [Test]
    public async Task SignUpUser_CreateFails_ReturnsFailed()
    {
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Users>(), _newPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Creation failed" }));
            
        var result = await _identityService.SignUpUserAsync(_newEmail, _newPassword, _firstName, _lastName, _phoneNumber);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Creation failed"));
            Assert.That(result.token, Is.Empty);
        });
    }

    [Test]
    public async Task SignUpUser_RoleAddFails_ReturnsFailedAndDeletesUser()
    {
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<Users>(), Roles.User))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role add failed" }));
            
        var result = await _identityService.SignUpUserAsync(_newEmail, _newPassword, _firstName, _lastName, _phoneNumber);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Role add failed"));
            Assert.That(result.token, Is.Empty);
        });
        
        _mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<Users>()), Times.Once);
    }

    [Test]
    public async Task SignUpUser_ClaimAddFails_ReturnsFailedAndCleansUp()
    {
        _mockUserManager.Setup(x => x.AddClaimAsync(It.IsAny<Users>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Claim add failed" }));
            
        var result = await _identityService.SignUpUserAsync(_newEmail, _newPassword, _firstName, _lastName, _phoneNumber);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Claim add failed"));
            Assert.That(result.token, Is.Empty);
        });
        
        _mockUserManager.Verify(x => x.RemoveFromRoleAsync(It.IsAny<Users>(), Roles.User), Times.Once);
        _mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<Users>()), Times.Once);
    }

    [Test]
    public async Task ValidateSignup_UserNotFound_ReturnsFailed()
    {
        var result = await _identityService.ValidateSignupAsync(_invalidUserId.ToString(), _activationToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Invalid user"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateSignup_NotPendingUser_ReturnsFailed()
    {
        _validUser.UsersStatus = StatusEnum.Active;
        
        var result = await _identityService.ValidateSignupAsync(_validUserId.ToString(), _activationToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Invalid user"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateSignup_AlreadyActivated_ReturnsFailed()
    {
        _alreadyActiveUser.UsersStatus = StatusEnum.Pending;
        var result = await _identityService.ValidateSignupAsync(_alreadyActiveUserId.ToString(), _activationToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("User already activated"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateSignup_ConfirmationFails_ReturnsFailed()
    {
        _updateFailUser.UsersStatus = StatusEnum.Pending;
        var result = await _identityService.ValidateSignupAsync(_updateFailUserId.ToString(), _activationToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Confirmation failed"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateSignup_Success_ReturnsSuccessWithEmail()
    {
        var result = await _identityService.ValidateSignupAsync(_validUserId.ToString(), _activationToken);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.True);
            Assert.That(result.Item1.Message, Is.EqualTo(ResultMessage.SignUpSuccess));
            Assert.That(result.usersEmail, Is.EqualTo(_validUser.Email));
            Assert.That(_validUser.UsersStatus, Is.EqualTo(StatusEnum.Active));
        });
        
        _mockUserManager.Verify(x => x.ConfirmEmailAsync(_validUser, _activationToken), Times.Once);
        _mockUserManager.Verify(x => x.UpdateAsync(_validUser), Times.Once);
    }
}