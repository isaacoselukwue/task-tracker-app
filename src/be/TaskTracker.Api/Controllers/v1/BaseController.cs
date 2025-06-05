global using Asp.Versioning;

namespace TaskTracker.Api.Controllers.v1;
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
public class BaseController : ControllerBase
{
}
