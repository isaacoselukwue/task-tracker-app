namespace TaskTracker.Tests.Accounts.Services;

class IdentityServiceTests_Queries : IdentityServiceTests
{
    private IQueryable<Users> _usersQueryable;

    [SetUp]
    public new void Setup()
    {
        base.Setup();
        _validUser.FirstName = "Valid";
        _validUser.LastName = "User";
        _validUser.PhoneNumber = "1234567890";
        _validUser.UsersStatus = StatusEnum.Active;
        _alreadyActiveUser.FirstName = "Active";
        _alreadyActiveUser.LastName = "User";
        _alreadyActiveUser.PhoneNumber = "0987654321";
        List<Users> users = [_validUser, _alreadyActiveUser, _updateFailUser];

        _usersQueryable = users.AsAsyncQueryable();

        List<UserRoles> roles = [new() { Id = Guid.NewGuid(), Name = "Admin" }, new() { Id = Guid.NewGuid(), Name = "User" }];

        List<IdentityUserRole<Guid>> userRoles = [
            new() { UserId = _validUserId, RoleId = roles[0].Id },
            new() { UserId = _validUserId, RoleId = roles[1].Id },
            new() { UserId = _alreadyActiveUserId, RoleId = roles[1].Id }];

        _mockUserManager.Setup(x => x.Users).Returns(_usersQueryable);

        _mockDbContext.Setup(x => x.Users).Returns(DbSetMockProvider.GetMockDbSet(users.AsQueryable()));
        _mockDbContext.Setup(x => x.Roles).Returns(DbSetMockProvider.GetMockDbSet(roles.AsQueryable()));
        _mockDbContext.Setup(x => x.UserRoles).Returns(DbSetMockProvider.GetMockDbSet(userRoles.AsQueryable()));
    }

    [Test]
    public void UserAccounts_ReturnsAllUsers()
    {
        var result = _identityService.UserAccounts();

        Assert.Multiple(() =>
        {
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result.Any(u => u.Id == _validUserId), Is.True);
            Assert.That(result.Any(u => u.Id == _alreadyActiveUserId), Is.True);
            Assert.That(result.Any(u => u.Id == _updateFailUserId), Is.True);
        });
    }

    [Test]
    public async Task UserAccountWithRoles_ReturnsCorrectData()
    {
        var userAccounts = _identityService.UserAccountWithRoles().AsAsyncQueryable();

        var results = await userAccounts.ToListAsync();
        Assert.That(results, Has.Count.EqualTo(2));

        var firstUser = results.First(u => u.UserId == _validUserId);
        Assert.Multiple(() =>
        {
            Assert.That(firstUser.FirstName, Is.EqualTo("Valid"));
            Assert.That(firstUser.LastName, Is.EqualTo("User"));
            Assert.That(firstUser.EmailAddress, Is.EqualTo("valid@example.com"));
            Assert.That(firstUser.PhoneNumber, Is.EqualTo("1234567890"));
            Assert.That(firstUser.Status, Is.EqualTo(StatusEnum.Active));
            Assert.That(firstUser.Roles, Has.Count.EqualTo(2));
            Assert.That(firstUser.Roles, Contains.Item("Admin"));
            Assert.That(firstUser.Roles, Contains.Item("User"));
        });

        var secondUser = results.First(u => u.UserId == _alreadyActiveUserId);
        Assert.Multiple(() =>
        {
            Assert.That(secondUser.FirstName, Is.EqualTo("Active"));
            Assert.That(secondUser.LastName, Is.EqualTo("User"));
            Assert.That(secondUser.EmailAddress, Is.EqualTo("active@example.com"));
            Assert.That(secondUser.PhoneNumber, Is.EqualTo("0987654321"));
            Assert.That(secondUser.Status, Is.EqualTo(StatusEnum.Active));
            Assert.That(secondUser.Roles, Has.Count.EqualTo(1));
            Assert.That(secondUser.Roles, Contains.Item("User"));
        });
    }
}
public static class DbSetMockProvider
{
    public static DbSet<T> GetMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet.Object;
    }
}
