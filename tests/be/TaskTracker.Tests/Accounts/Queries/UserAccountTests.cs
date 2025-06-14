namespace TaskTracker.Tests.Accounts.Queries;

[TestFixture]
class UserAccountTests
{
    private Mock<IIdentityService> _mockIdentityService;
    private UserAccountQuery invalidZeroPageNumberCommand;
    private UserAccountQuery invalidZeroPageCountCommand;
    private UserAccountQuery validCommand;
    private UserAccountValidator _validator;
    private UserAccountQueryHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new();
        _validator = new();

        invalidZeroPageNumberCommand = new() { PageNumber = 0, PageCount = 10 };
        invalidZeroPageCountCommand = new() { PageNumber = 1, PageCount = 0 };
        validCommand = new() { PageNumber = 1, PageCount = 10 };

        _handler = new(_mockIdentityService.Object);
    }

    [Test]
    public async Task UserAccount_ZeroPageNumber_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidZeroPageNumberCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task UserAccount_ZeroPageCount_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidZeroPageCountCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task UserAccount_ValidParams_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task UserAccount_EmptyResults_ReturnsEmptyList()
    {
        var emptyList = new List<UserAccountResult>().AsAsyncQueryable();

        _mockIdentityService.Setup(x => x.UserAccountWithRoles())
            .Returns(emptyList);

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.Results, Is.Empty);
            Assert.That(result.Data?.TotalResults, Is.EqualTo(0));
            Assert.That(result.Data?.TotalPages, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task UserAccount_SinglePage_ReturnsCorrectPagination()
    {
        var testData = GetTestUserAccounts(5).AsAsyncQueryable();

        _mockIdentityService.Setup(x => x.UserAccountWithRoles()).Returns(testData);

        validCommand.PageNumber = 1;
        validCommand.PageCount = 10;

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.Results, Has.Count.EqualTo(5));
            Assert.That(result.Data?.TotalResults, Is.EqualTo(5));
            Assert.That(result.Data?.TotalPages, Is.EqualTo(1));
            Assert.That(result.Data?.Page, Is.EqualTo(1));
            Assert.That(result.Data?.Size, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task UserAccount_MultiplePages_ReturnsCorrectPage()
    {
        var testData = GetTestUserAccounts(25).AsAsyncQueryable();

        _mockIdentityService.Setup(x => x.UserAccountWithRoles()).Returns(testData);

        validCommand.PageNumber = 2;
        validCommand.PageCount = 10;

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.Results, Has.Count.EqualTo(10));
            Assert.That(result.Data?.TotalResults, Is.EqualTo(25));
            Assert.That(result.Data?.TotalPages, Is.EqualTo(3));
            Assert.That(result.Data?.Page, Is.EqualTo(2));
            Assert.That(result.Data?.Size, Is.EqualTo(10));
            Assert.That(result.Data?.Results.First().EmailAddress, Is.EqualTo("user10@example.com"));
            Assert.That(result.Data?.Results.Last().EmailAddress, Is.EqualTo("user19@example.com"));
        });
    }

    [Test]
    public async Task UserAccount_LastPage_ReturnsRemainingItems()
    {
        var testData = GetTestUserAccounts(25).AsAsyncQueryable();

        _mockIdentityService.Setup(x => x.UserAccountWithRoles()).Returns(testData);

        validCommand.PageNumber = 3;
        validCommand.PageCount = 10;

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.Results, Has.Count.EqualTo(5));
            Assert.That(result.Data?.TotalResults, Is.EqualTo(25));
            Assert.That(result.Data?.TotalPages, Is.EqualTo(3));
            Assert.That(result.Data?.Page, Is.EqualTo(3));
            Assert.That(result.Data?.Size, Is.EqualTo(10));
            Assert.That(result.Data?.Results.First().EmailAddress, Is.EqualTo("user20@example.com"));
            Assert.That(result.Data?.Results.Last().EmailAddress, Is.EqualTo("user24@example.com"));
        });
    }

    private static List<UserAccountResult> GetTestUserAccounts(int count)
    {
        List<UserAccountResult> results = [];

        for (int i = 0; i < count; i++)
        {
            results.Add(new UserAccountResult
            {
                UserId = Guid.NewGuid(),
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                EmailAddress = $"user{i}@example.com",
                PhoneNumber = $"555-{i:000}-{i:000}",
                DateAccountCreated = DateTimeOffset.UtcNow.AddDays(-count + i),
                Status = StatusEnum.Active,
                Roles = ["User"]
            });
        }

        return results;
    }
}