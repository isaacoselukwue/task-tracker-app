global using Microsoft.AspNetCore.Identity;

namespace TaskTracker.Tests.Accounts.Services;

partial class IdentityServiceTests_UserRoles : IdentityServiceTests
{
    private string _validRole;
    private string _invalidRole;

    [SetUp]
    public new void Setup()
    {
        base.Setup();

        _validRole = "User";
        _invalidRole = "NonExistentRole";

        _mockUserManager.Setup(x => x.GetRolesAsync(_validUser)).ReturnsAsync(["Admin"]);

        _mockUserManager.Setup(x => x.GetRolesAsync(_alreadyActiveUser)).ReturnsAsync(["User"]);

        _mockUserManager.Setup(x => x.GetRolesAsync(_updateFailUser)).ReturnsAsync(["User"]);

        _mockUserManager.Setup(x => x.AddToRoleAsync(_validUser, _validRole)).ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<Users>(), _invalidRole))
            .ReturnsAsync(IdentityResult.Failed(new Microsoft.AspNetCore.Identity.IdentityError { Description = "Role does not exist" }));
    }

    [Test]
    public async Task ChangeUserRole_UserNotFound_ReturnsFailed()
    {
        var result = await _identityService.ChangeUserRoleAsync(_invalidUserId.ToString(), _validRole);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Invalid user"));
            Assert.That(result.email, Is.Empty);
        });
    }

    [Test]
    public async Task ChangeUserRole_InvalidRole_ReturnsFailed()
    {
        _validUser.UsersStatus = StatusEnum.Active;
        var result = await _identityService.ChangeUserRoleAsync(_validUserId.ToString(), _invalidRole);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Role does not exist"));
            Assert.That(result.email, Is.Empty);
        });
    }

    [Test]
    public async Task ChangeUserRole_Success_ReturnsSuccessWithEmail()
    {
        _validUser.UsersStatus = StatusEnum.Active;
        var result = await _identityService.ChangeUserRoleAsync(_validUserId.ToString(), _validRole);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.True);
            Assert.That(result.Item1.Message, Is.EqualTo(ResultMessage.ChangeUserRoleSuccess));
            Assert.That(result.email, Is.EqualTo(_validUser.Email));
        });

        _mockUserManager.Verify(x => x.AddToRoleAsync(_validUser, _validRole), Times.Once);
    }

    [Test]
    public async Task DeactivateAccount_UserNotFound_ReturnsFailed()
    {
        var result = await _identityService.DeactivateAccountAsync(_invalidUserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Invalid user"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task DeactivateAccount_AlreadyInactive_ReturnsFailed()
    {
        _validUser.UsersStatus = StatusEnum.InActive;

        var result = await _identityService.DeactivateAccountAsync(_validUserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Account is not active"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task DeactivateAccount_UpdateFails_ReturnsFailed()
    {
        _updateFailUser.UsersStatus = StatusEnum.Active;

        var result = await _identityService.DeactivateAccountAsync(_updateFailUserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Update failed"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task DeactivateAccount_Success_ReturnsSuccessWithEmail()
    {
        _validUser.UsersStatus = StatusEnum.Active;

        var result = await _identityService.DeactivateAccountAsync(_validUserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.True);
            Assert.That(result.Item1.Message, Is.EqualTo(ResultMessage.DeactivateAccountSuccess));
            Assert.That(result.usersEmail, Is.EqualTo(_validUser.Email));
            Assert.That(_validUser.UsersStatus, Is.EqualTo(StatusEnum.InActive));
            Assert.That(_validUser.LastModifiedBy, Is.EqualTo(_currentUserEmail));
        });

        _mockUserManager.Verify(x => x.UpdateAsync(_validUser), Times.Once);
    }
}
