using TaskTracker.Application.TaskReminders.Commands;
using TaskTracker.Application.TaskReminders.Queries;

namespace TaskTracker.Api.Controllers.v1;
[Authorize]
public class TasksController(ISender sender) : BaseController
{
    [HttpPost("create")]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> Create([FromBody] CreateTaskCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded? StatusCode(StatusCodes.Status201Created, result) : BadRequest(result);
    }

    [HttpGet("upcoming")]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result<UpcomingTasksDto>>> UpcomingTasks([FromQuery] GetUpcomingTasksQuery query)
    {
        var result = await sender.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("mark-completed")]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> MarkTaskAsDone([FromBody] MarkTaskAsDoneCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? StatusCode(StatusCodes.Status202Accepted, result) : BadRequest(result);
    }

    [HttpDelete("delete")]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> DeleteTask([FromBody] DeleteTaskCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("users-tasks")]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> UserTasks([FromQuery] UserTasksQuery query)
    {
        var result = await sender.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("admin/all-tasks")]
    [Authorize(Policy = "UserPolicy")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(typeof(Result), 400)]
    public async ValueTask<ActionResult<Result>> AdminTasks([FromQuery] AdminTasksQuery query)
    {
        var result = await sender.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
