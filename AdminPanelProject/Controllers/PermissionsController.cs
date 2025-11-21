using AdminPanelProject.Authorization;
using AdminPanelProject.Data;
using AdminPanelProject.Dtos.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminPanelProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PermissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/permissions/catalog
        [HttpGet("catalog")]
        [HasPermission("Roles.List")]
        public async Task<IActionResult> GetCatalog()
        {
            var permissions = await _context.Permissions
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description
                })
                .ToListAsync();

            return Ok(permissions);
        }
    }
}
