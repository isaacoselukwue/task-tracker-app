namespace TaskTracker.Tests.Authentication.Commands;
[TestFixture]
class SignupTests
{
    private Mock<IIdentityService> _mockIdentityService;
    private Mock<IPublisher> _mockPublisher;
    private SignupCommand emptyEmailCommand;
    private SignupCommand passwordMismatchCommand;
    private SignupCommand invalidPhoneCommand;
    private SignupCommand validCommand;
    private SignupValidator _validator;
    private SignupCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new();
        _mockPublisher = new();
        _validator = new();

        emptyEmailCommand = new()
        {
            EmailAddress = "",
            Password = "ValidPass123!",
            ConfirmPassword = "ValidPass123!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "07123456789"
        };

        passwordMismatchCommand = new()
        {
            EmailAddress = "test@example.com",
            Password = "ValidPass123!",
            ConfirmPassword = "DifferentPass123!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "07123456789"
        };

        invalidPhoneCommand = new()
        {
            EmailAddress = "test@example.com",
            Password = "ValidPass123!",
            ConfirmPassword = "ValidPass123!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "phone123"
        };

        validCommand = new()
        {
            EmailAddress = "test@example.com",
            Password = "ValidPass123!",
            ConfirmPassword = "ValidPass123!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "07123456789"
        };

        _handler = new(_mockIdentityService.Object, _mockPublisher.Object);
    }

    [Test]
    public async Task Signup_EmptyEmail_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyEmailCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Has.Some.Matches<FluentValidation.Results.ValidationFailure>(f => f.PropertyName == "EmailAddress"));
        });
    }

    [Test]
    public async Task Signup_PasswordMismatch_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(passwordMismatchCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Has.Some.Matches<FluentValidation.Results.ValidationFailure>(f => f.PropertyName == "ConfirmPassword"));
        });
    }

    [Test]
    public async Task Signup_InvalidPhoneNumber_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidPhoneCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Has.Some.Matches<FluentValidation.Results.ValidationFailure>(f => f.PropertyName == "PhoneNumber"));
        });
    }

    [Test]
    public async Task Signup_ValidData_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task Signup_EmailAlreadyExists_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.SignUpUserAsync(
            validCommand.EmailAddress!,
            validCommand.Password!,
            validCommand.FirstName!,
            validCommand.LastName!,
            validCommand.PhoneNumber!))
        .ReturnsAsync((Result.Failure(ResultMessage.SignUpFailed, ["User with this email already exists"]), string.Empty));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("User with this email already exists"));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Signup_ValidCredentials_ReturnsSuccess_AndPublishesNotification()
    {
        string userId = "user-guid";
        string token = "activation-token";

        _mockIdentityService.Setup(x => x.SignUpUserAsync(
            validCommand.EmailAddress!,
            validCommand.Password!,
            validCommand.FirstName!,
            validCommand.LastName!,
            validCommand.PhoneNumber!))
        .ReturnsAsync((Result.Success(userId), token));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Contains.Substring("Signup successful"));
        });

        _mockPublisher.Verify(p => p.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == validCommand.EmailAddress &&
                n.Subject == "Account Activation!" &&
                n.NotificationType == NotificationTypeEnum.SignUpAccountActivation &&
                n.Replacements.Count == 2 &&
                n.Replacements.ContainsKey("{{token}}") &&
                n.Replacements.ContainsKey("{{userid}}")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}