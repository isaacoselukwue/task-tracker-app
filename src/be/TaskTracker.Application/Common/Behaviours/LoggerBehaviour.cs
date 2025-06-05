global using MediatR.Pipeline;
global using Microsoft.Extensions.Logging;

namespace TaskTracker.Application.Common.Behaviours;
public class LoggerBehaviour<TRequest>(ICurrentUser user, ILogger<TRequest> logger) : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger = logger;
    private readonly ICurrentUser _user = user;
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        Guid userId = _user.UserId;
        string requestName = typeof(TRequest).Name;
        _logger.LogInformation("TaskTracker.Api New Request: {Name} {@UserId} {@Request}", requestName, userId, request);
        return Task.CompletedTask;
    }
}