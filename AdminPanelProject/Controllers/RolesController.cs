using AdminPanelProject.Authorization;
using AdminPanelProject.Dtos;
using AdminPanelProject.Dtos.Roles;
using AdminPanelProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace AdminPanelProject.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class rolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly IAuditLogService _auditLogService;

        public rolesController(IRoleService roleService, IAuditLogService auditLogService)
        {
            _roleService = roleService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<string> count()
        {
            string no = await _roleService.countAsync();
            return no;
        }


        // GET: api/roles?name=Admin&description=manager&isActive=true
        [HttpGet]
        [HasPermission("Roles.List")]
        public async Task<IActionResult> GetRoles(
    [FromQuery] string? name,
    [FromQuery] string? description,
    [FromQuery] bool? isActive,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? sortField = "name",
    [FromQuery] string? sortDirection = "asc")
        {
            try
            {
                string[] allowedSort = {  "name", "description", "isactive", "createdon" };
                string? isActiveRaw = HttpContext.Request.Query["isActive"].ToString();
                var validation = QueryValidator.ValidateQuery(pageNumber, pageSize, sortField, sortDirection, allowedSort, isActiveRaw);

                if (!validation.IsValid)
                {
                    return BadRequest(new { message = validation.ErrorMessage });
                }

                description = description?.Trim();
            var roles = await _roleService.GetRolesAsync(name, description, isActive, pageNumber, pageSize, sortField, sortDirection);
            var firstName = User.FindFirst("FirstName")?.Value;
            var lastName = User.FindFirst("LastName")?.Value;
            var fullName = $"{firstName} {lastName}".Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                fullName = User.Identity?.Name ?? "Unknown";
            await _auditLogService.LogAsync(fullName, "View", "Viewed Roles List");
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    code = 500,
                    message = "An error occurred while retrieving roles.",
                    details = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        [HasPermission("Roles.List")]
        public async Task<IActionResult> GetRole(Guid id)
        {
            

            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null) return NotFound(new {message = $"Role with ID {id} not found."});

            var firstName = User.FindFirst("FirstName")?.Value;
            var lastName = User.FindFirst("LastName")?.Value;
            var fullName = $"{firstName} {lastName}".Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                fullName = User.Identity?.Name ?? "Unknown";

            //await _auditLogService.LogAsync(fullName, "View", $"Viewed Role {role.Name}");
            await _auditLogService.LogAsync(fullName, "View", $"Viewed Role Details");
            return Ok(role);
            
           
        }

        [HttpPost]
        [HasPermission("Roles.Add")]
        public async Task<IActionResult> CreateRole([FromBody] RoleDto dto)
        {
            var validationError = ValidateRoleDto(dto);
            if (!string.IsNullOrEmpty(validationError))
            {
                return BadRequest(new { message = validationError });
            }
            var createdBy = User?.Identity?.Name ?? "System";
            var (success , errorMessage)= await _roleService.CreateRoleAsync(dto, createdBy);
            if (!success)
            {
                return BadRequest(new { message = errorMessage ?? "Failed to create role" });
            }
            var firstName = User.FindFirst("FirstName")?.Value;
            var lastName = User.FindFirst("LastName")?.Value;
            var fullName = $"{firstName} {lastName}".Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                fullName = User.Identity?.Name ?? "Unknown";

            //await _auditLogService.LogAsync(fullName, "Create", $"Created Role {dto.Name}");
            await _auditLogService.LogAsync(fullName, "Create", $"Role created successfully");

            return success ? Ok(new { message = "Role created successfully" }) : BadRequest("Failed to create role");
        }

        [HttpPut("{id}")]
        [HasPermission("Roles.Edit")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] RoleDto dto)
        {
            var validationError = ValidateRoleDto(dto);
            if (!string.IsNullOrEmpty(validationError))
            {
                return BadRequest(new { message = validationError });
            }
            var modifiedBy = User?.Identity?.Name ?? "System";
            var result = await _roleService.UpdateRoleAsync(id, dto, modifiedBy);

            var currentUser = HttpContext.User;
            var currentUserRoleName = currentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            bool affectsCurrentUser = string.Equals(currentUserRoleName, result.RoleName, StringComparison.OrdinalIgnoreCase);

            if (result.Success)
            {
              var firstName = User.FindFirst("FirstName")?.Value;
              var lastName = User.FindFirst("LastName")?.Value;
              var fullName = $"{firstName} {lastName}".Trim();

              if (string.IsNullOrWhiteSpace(fullName))
                  fullName = User.Identity?.Name ?? "Unknown";
                //await _auditLogService.LogAsync(fullName, "Update", $"Attempted to update Role {dto.Name}");
                await _auditLogService.LogAsync(fullName, "Update", $"Role updated successfully");

                return Ok(new { message = "Role updated successfully", affectsCurrentUser });
            }

            // --- Consistent structured error handling ---
            if (result.ErrorMessage == "Role not found")
                return NotFound(new { message = result.ErrorMessage }); // 404

            if (result.ErrorMessage?.StartsWith("Cannot modify protected role") == true)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }); // 403

            return StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage ?? "An unexpected error occurred", affectsCurrentUser });
        }


        [HttpDelete("{id}")]
        [HasPermission("Roles.Delete")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            var (success, error) = await _roleService.DeleteRoleAsync(id);



            if (success)
            {
                var firstName = User.FindFirst("FirstName")?.Value;
                var lastName = User.FindFirst("LastName")?.Value;
                var fullName = $"{firstName} {lastName}".Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = User.Identity?.Name ?? "Unknown";

                //await _auditLogService.LogAsync(fullName, "Delete", $"Deleted Role {id}");
                await _auditLogService.LogAsync(fullName, "Delete", $"Deleted Role successfully");

                return Ok(new { message = "Role deleted successfully" });
            }
            if (error == "Role not found") return NotFound(new { message = error });

            // 409 Conflict when role has users
            if (error?.StartsWith("Cannot delete protected role") == true)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = error }); // 403

            if (error?.StartsWith("Cannot delete role - one or more users") == true)
                return Conflict(new { message = error }); // 409

            // Default fallback
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = error ?? "An unexpected error occurred" });
        }


        private string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        [HttpGet("export")]
        [HasPermission("Roles.List")]
        public async Task<IActionResult> Export()
        {
            try
            {
                var firstName = User.FindFirst("FirstName")?.Value;
                var lastName = User.FindFirst("LastName")?.Value;
                var fullName = $"{firstName} {lastName}".Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = User.Identity?.Name ?? "Unknown";

                await _auditLogService.LogAsync(fullName, "Export", "Exported Roles CSV");

                return Ok(new { message = "Audit log recorded for roles CSV export" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Failed to log roles export.",
                    details = ex.Message
                });
            }
        }


        [HttpPatch("{id}/toggle-status")]
        [HasPermission("Roles.Edit")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var firstName = User.FindFirst("FirstName")?.Value;
            var lastName = User.FindFirst("LastName")?.Value;
            var fullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = User.Identity?.Name ?? "Unknown";

            var result = await _roleService.ToggleStatusAsync(id, fullName);
            if (!result) return NotFound(new { message = "Role not found" });

            await _auditLogService.LogAsync(fullName, "Update", $"Updated role status  ");
            return Ok(new { message = "Role status updated successfully" });
        }






        private string? ValidateRoleDto(RoleDto dto)
        {
            if (dto == null) return "Role data is required.";
            var description = dto.Description?.Trim();
           
            if (string.IsNullOrWhiteSpace(description))
                return "Role description is required.";

            if (description.Length < 2)
                return "Role description must be at least 2 characters long.";

            if (description.Length > 50)
                return "Role description cannot exceed 50 characters.";

            // Check for more than one consecutive space
            if (System.Text.RegularExpressions.Regex.IsMatch(description, @"\s{2,}"))
                return "Role description cannot contain multiple consecutive spaces.";

            if (dto.PermissionIds == null || !dto.PermissionIds.Any())
                return "At least one permission must be selected";


            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return "Role name is required.";

            if (name.Length < 2)
                return "Role name must be at least 2 characters long.";

            if (name.Length > 50)
                return "Role name cannot exceed 50 characters.";

            // Regex: only letters (A-Z, a-z) and at most one space between words
            // ^[A-Za-z]+( [A-Za-z]+)?$ → allows "Admin" or "Role Name" but not "Role  Name" or "Role1"
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z]+( [A-Za-z]+)*$")
     || System.Text.RegularExpressions.Regex.IsMatch(name, @"\s{2,}"))
                return "Role name can only contain alphabets and a single space between words.";

            return null; // valid output
        }

    }
}
