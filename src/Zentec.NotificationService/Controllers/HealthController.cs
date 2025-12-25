using Microsoft.AspNetCore.Mvc;

namespace Zentec.NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "ok", service = "notification" });
    }
}
