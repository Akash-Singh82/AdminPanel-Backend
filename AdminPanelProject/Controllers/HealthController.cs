using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }
    }
}
