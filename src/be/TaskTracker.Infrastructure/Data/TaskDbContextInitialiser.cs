global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using System.Security.Claims;
global using TaskTracker.Domain.Constants;
global using TaskTracker.Domain.Enums;

namespace TaskTracker.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<TaskDbContextInitialiser>();

        await initialiser.InitialiseAsync();

        await initialiser.SeedAsync();
    }
}

internal class TaskDbContextInitialiser(ILogger<TaskDbContextInitialiser> logger, TaskDbContext context, UserManager<Users> userManager, RoleManager<UserRoles> roleManager)
{
    private readonly ILogger<TaskDbContextInitialiser> _logger = logger;
    private readonly TaskDbContext _context = context;
    private readonly UserManager<Users> _userManager = userManager;
    private readonly RoleManager<UserRoles> _roleManager = roleManager;

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        List<UserRoles> defaultRoles = [
            new UserRoles { Name = Roles.Admin, UserRoleStatus = StatusEnum.Active },
            new UserRoles { Name = Roles.User, UserRoleStatus = StatusEnum.Active }
            ];

        foreach (UserRoles role in defaultRoles)
        {
            if (_roleManager.Roles.All(r => r.Name != role.Name))
            {
                await _roleManager.CreateAsync(role);
            }
        }


        Users administrator = new()
        {
            Created = DateTimeOffset.UtcNow,
            CreatedBy = "System",
            Email = "princeizak@live.com",
            EmailConfirmed = true,
            FirstName = "Isaac",
            LastModified = DateTimeOffset.UtcNow,
            LastName = "Ose",
            PhoneNumber = "07000000000",
            PhoneNumberConfirmed = true,
            UsersStatus = StatusEnum.Active
        };
        administrator.UserName = administrator.Email;

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            IdentityResult identityResult = await _userManager.CreateAsync(administrator, "Administrator1!");
            _logger.LogInformation("Result from created admin is: {identityResult}", identityResult);
            List<string> availableRoles = [.. defaultRoles.Select(r => r.Name).Where(roleName => !string.IsNullOrWhiteSpace(roleName))];
            identityResult = await _userManager.AddToRolesAsync(administrator, availableRoles);
            _logger.LogInformation("Result from adding admin to roles is: {identityResult}", identityResult);
            identityResult = await _userManager.AddClaimAsync(administrator, new Claim("Permission", "CanView"));
            _logger.LogInformation("Result from adding claims to admin is: {identityResult}", identityResult);
        }
    }
}
