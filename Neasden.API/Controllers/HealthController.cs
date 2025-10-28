using Microsoft.AspNetCore.Mvc;

namespace Neasden.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("API is live!");
}
