global using FluentValidation;
global using MediatR;
global using Microsoft.EntityFrameworkCore;
global using TaskTracker.Application.Common.Interfaces;
global using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Accounts.Queries;
public record UserAccountQuery : IRequest<Result<UserAccountDto>>
{
    public int PageNumber { get; set; }
    public int PageCount { get; set; }
}

public class UserAccountValidator : AbstractValidator<UserAccountQuery>
{
    public UserAccountValidator()
    {
        RuleFor(v => v.PageNumber).GreaterThan(0);
        RuleFor(v => v.PageCount).GreaterThan(0);
    }
}

public class UserAccountQueryHandler(IIdentityService identityService) : IRequestHandler<UserAccountQuery, Result<UserAccountDto>>
{
    public async Task<Result<UserAccountDto>> Handle(UserAccountQuery request, CancellationToken cancellationToken)
    {
        IQueryable<UserAccountResult> userAccounts = identityService.UserAccountWithRoles();
        int totalResults = await userAccounts.CountAsync(cancellationToken: cancellationToken);
        int totalPages = (int)Math.Ceiling((double)totalResults / request.PageCount);

        List<UserAccountResult> result = await userAccounts.Select(x => new UserAccountResult
        {
            DateAccountCreated = x.DateAccountCreated,
            EmailAddress = x.EmailAddress,
            FirstName = x.FirstName,
            LastName = x.LastName,
            PhoneNumber = x.PhoneNumber,
            Status = x.Status,
            UserId = x.UserId,
            Roles = x.Roles
        })
                .Skip((request.PageNumber - 1) * request.PageCount)
                .Take(request.PageCount)
                .ToListAsync(cancellationToken: cancellationToken);

        UserAccountDto userAccountResult = new()
        {
            Page = request.PageNumber,
            Size = request.PageCount,
            TotalPages = totalPages,
            TotalResults = totalResults,
            Results = result
        };
        return Result<UserAccountDto>.Success("User accounts retrieved successfully.", userAccountResult);
    }
}


public class UserAccountDto
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
    public List<UserAccountResult> Results { get; set; } = [];
}
public class UserAccountResult
{

    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTimeOffset DateAccountCreated { get; set; }
    public StatusEnum Status { get; set; }
    public List<string> Roles { get; set; } = [];
}