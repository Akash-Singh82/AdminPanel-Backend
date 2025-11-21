using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AdminPanelProject.Models;
using Microsoft.AspNetCore.Authorization;
using AdminPanelProject.Authorization;

namespace AdminPanelProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class RemoteValidationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RemoteValidationController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet("is-email-available")]
        [HasPermission("Users.List")]
        public async Task<IActionResult> IsEmailAvailable(string Email)
        {
            if(string.IsNullOrWhiteSpace(Email))
            {
                return BadRequest(new { available = false });
            }

            var user = await _userManager.FindByEmailAsync(Email);
            bool available = (user == null);
            return Ok(new {available });
        }
    }
}
