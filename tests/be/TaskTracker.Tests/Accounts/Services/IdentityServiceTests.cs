namespace TaskTracker.Tests.Accounts.Services;

[TestFixture]
abstract class IdentityServiceTests
{
    protected Mock<UserManager<Users>> _mockUserManager;
    protected Mock<SignInManager<Users>> _mockSignInManager;
    protected Mock<IJwtService> _mockJwtService;
    protected Mock<ITaskDbContext> _mockDbContext;
    protected IdentityService _identityService;
    protected Guid _validUserId;
    protected Guid _invalidUserId;
    protected Guid _alreadyActiveUserId;
    protected Guid _updateFailUserId;
    protected string _currentUserEmail;
    protected string _newPassword;
    protected Users _validUser;
    protected Users _alreadyActiveUser;
    protected Users _updateFailUser;

    [SetUp]
    public void Setup()
    {
        _validUserId = Guid.NewGuid();
        _invalidUserId = Guid.NewGuid();
        _alreadyActiveUserId = Guid.NewGuid();
        _updateFailUserId = Guid.NewGuid();
        _currentUserEmail = "current@example.com";
        _newPassword = "NewSecurePassword123!";

        _mockUserManager = MockUserManager();
        _mockSignInManager = MockSignInManager();
        _mockJwtService = new();
        _mockDbContext = new();
        _mockDbContext.Setup(x => x.PasswordHistories.AddAsync(It.IsAny<PasswordHistories>(), It.IsAny<CancellationToken>()));
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _validUser = new Users
        {
            Id = _validUserId,
            Email = "valid@example.com",
            UsersStatus = StatusEnum.InActive,
            PasswordHash = "OldPasswordHash"
        };

        _alreadyActiveUser = new Users
        {
            Id = _alreadyActiveUserId,
            Email = "active@example.com",
            UsersStatus = StatusEnum.Active
        };

        _updateFailUser = new Users
        {
            Id = _updateFailUserId,
            Email = "fail@example.com",
            UsersStatus = StatusEnum.InActive
        };

        _mockJwtService.Setup(x => x.GetUserId()).Returns(_validUserId);
        _mockJwtService.Setup(x => x.GetEmailAddress()).Returns(_currentUserEmail);

        _mockUserManager.Setup(x => x.FindByIdAsync(_validUserId.ToString()))
            .ReturnsAsync(_validUser);

        _mockUserManager.Setup(x => x.FindByIdAsync(_alreadyActiveUserId.ToString()))
            .ReturnsAsync(_alreadyActiveUser);

        _mockUserManager.Setup(x => x.FindByIdAsync(_updateFailUserId.ToString()))
            .ReturnsAsync(_updateFailUser);

        _mockUserManager.Setup(x => x.FindByIdAsync(_invalidUserId.ToString()))
            .ReturnsAsync((Users?)null);

        _mockUserManager.Setup(x => x.UpdateAsync(_validUser))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.UpdateAsync(_updateFailUser))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

        _mockUserManager.Setup(x => x.ChangePasswordAsync(_validUser, _validUser.PasswordHash, _newPassword))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.ChangePasswordAsync(
           It.Is<Users>(u => u.Id == _validUserId && u.UsersStatus == StatusEnum.InActive),
           It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Account is not active" }));

        _mockUserManager.Setup(x => x.ChangePasswordAsync(
            It.Is<Users>(u => u.Id == _updateFailUserId),
            It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password change failed" }));

        _identityService = new(_mockSignInManager.Object, _mockUserManager.Object, _mockJwtService.Object, _mockDbContext.Object);
    }

    [Test]
    public async Task ActivateAccount_UserNotFound_ReturnsFailed()
    {
        var result = await _identityService.ActivateAccountAsync(_invalidUserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Invalid user"));
            Assert.That(result.email, Is.Empty);
        });
    }

    [Test]
    public async Task ActivateAccount_AlreadyActive_ReturnsFailed()
    {
        var result = await _identityService.ActivateAccountAsync(_alreadyActiveUserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Account is already active"));
            Assert.That(result.email, Is.Empty);
        });
    }

    [Test]
    public async Task ActivateAccount_UpdateFails_ReturnsFailed()
    {
        var result = await _identityService.ActivateAccountAsync(_updateFailUserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Update failed"));
            Assert.That(result.email, Is.Empty);
        });
    }

    [Test]
    public async Task ActivateAccount_Success_ReturnsSuccessWithEmail()
    {
        _validUser.UsersStatus = StatusEnum.Pending;
        var result = await _identityService.ActivateAccountAsync(_validUserId);
        
        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.True);
            Assert.That(result.Item1.Message, Is.EqualTo(ResultMessage.ActivateAccountSuccess));
            Assert.That(result.email, Is.EqualTo(_validUser.Email));
            Assert.That(_validUser.UsersStatus, Is.EqualTo(StatusEnum.Active));
            Assert.That(_validUser.LastModifiedBy, Is.EqualTo(_currentUserEmail));
        });

        _mockUserManager.Verify(x => x.UpdateAsync(_validUser), Times.Once);
    }

    [Test]
    public async Task ChangePassword_UserNotFound_ReturnsFailed()
    {
        _mockJwtService.Setup(x => x.GetUserId()).Returns(_invalidUserId);

        var result = await _identityService.ChangePasswordAsync(_newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Invalid user"));
            Assert.That(result.email, Is.Empty);
        });
    }

    [Test]
    public async Task ChangePassword_InactiveUser_ReturnsFailed()
    {
        _validUser.UsersStatus = StatusEnum.InActive;

        var result = await _identityService.ChangePasswordAsync(_newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Account is not active"));
            Assert.That(result.email, Is.Empty);
        });
    }

    [Test]
    public async Task ChangePassword_ChangePasswordFails_ReturnsFailed()
    {
        _updateFailUser.UsersStatus = StatusEnum.Active;
        _mockJwtService.Setup(x => x.GetUserId()).Returns(_updateFailUserId);

        var result = await _identityService.ChangePasswordAsync(_newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Password change failed"));
            Assert.That(result.email, Is.Empty);
        });
    }

    [Test]
    public async Task ChangePassword_Success_ReturnsSuccessWithEmail()
    {
        _validUser.UsersStatus = StatusEnum.Active;
        var result = await _identityService.ChangePasswordAsync(_newPassword);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.True);
            Assert.That(result.Item1.Message, Is.EqualTo(ResultMessage.ChangePasswordSuccess));
            Assert.That(result.email, Is.EqualTo(_validUser.Email));
        });

        _mockUserManager.Verify(x => x.ChangePasswordAsync(_validUser, _validUser.PasswordHash ?? "", _newPassword), Times.Once);
        _mockSignInManager.Verify(x => x.RefreshSignInAsync(_validUser), Times.Once);
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<UserManager<Users>> MockUserManager()
    {
        return new Mock<UserManager<Users>>(
            new Mock<IUserStore<Users>>().Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<Users>>().Object,
            Array.Empty<IUserValidator<Users>>(),
            Array.Empty<IPasswordValidator<Users>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<Users>>>().Object);
    }

    private static Mock<SignInManager<Users>> MockSignInManager()
    {
        return new Mock<SignInManager<Users>>(
            new Mock<UserManager<Users>>(
                new Mock<IUserStore<Users>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<Users>>().Object,
                Array.Empty<IUserValidator<Users>>(),
                Array.Empty<IPasswordValidator<Users>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<Users>>>().Object).Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<Users>>().Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<ILogger<SignInManager<Users>>>().Object,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<Users>>().Object);
    }
}
