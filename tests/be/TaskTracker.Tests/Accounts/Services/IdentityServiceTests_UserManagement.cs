namespace TaskTracker.Tests.Accounts.Services;

class IdentityServiceTests_UserManagement : IdentityServiceTests
{
    [SetUp]
    public new void Setup()
    {
        base.Setup();
        
        _mockUserManager.Setup(x => x.DeleteAsync(_validUser)).ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(x => x.DeleteAsync(_updateFailUser))
            .ReturnsAsync(IdentityResult.Failed(new Microsoft.AspNetCore.Identity.IdentityError { Description = "Delete failed" }));
    }

    [Test]
    public async Task DeleteUser_UserNotFound_ReturnsFailed()
    {
        var result = await _identityService.DeleteUserAsync(_invalidUserId.ToString(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Invalid user"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task DeleteUser_AlreadyDeleted_ReturnsFailed()
    {
        _validUser.UsersStatus = StatusEnum.Deleted;
        
        var result = await _identityService.DeleteUserAsync(_validUserId.ToString(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Account is already on soft delete"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task DeleteUser_SoftDeleteUpdateFails_ReturnsFailed()
    {
        var result = await _identityService.DeleteUserAsync(_updateFailUserId.ToString(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Update failed"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task DeleteUser_SoftDeleteSuccess_ReturnsSuccessWithEmail()
    {
        var result = await _identityService.DeleteUserAsync(_validUserId.ToString(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.True);
            Assert.That(result.Item1.Message, Is.EqualTo(ResultMessage.DeleteAccountSuccess));
            Assert.That(result.usersEmail, Is.EqualTo(_validUser.Email));
            Assert.That(_validUser.UsersStatus, Is.EqualTo(StatusEnum.Deleted));
            Assert.That(_validUser.LastModifiedBy, Is.EqualTo(_currentUserEmail));
        });

        _mockUserManager.Verify(x => x.UpdateAsync(_validUser), Times.Once);
    }

    [Test]
    public async Task DeleteUser_PermanentDeleteFails_ReturnsFailed()
    {
        var result = await _identityService.DeleteUserAsync(_updateFailUserId.ToString(), true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.False);
            Assert.That(result.Item1.Errors, Contains.Item("Delete failed"));
            Assert.That(result.usersEmail, Is.Empty);
        });
    }

    [Test]
    public async Task DeleteUser_PermanentDeleteSuccess_ReturnsSuccessWithEmail()
    {
        var result = await _identityService.DeleteUserAsync(_validUserId.ToString(), true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.Succeeded, Is.True);
            Assert.That(result.Item1.Message, Is.EqualTo(ResultMessage.DeleteAccountSuccess));
            Assert.That(result.usersEmail, Is.EqualTo(_validUser.Email));
        });

        _mockUserManager.Verify(x => x.DeleteAsync(_validUser), Times.Once);
    }
}