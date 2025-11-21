// Controllers/UsersController.cs
using AdminPanelProject.Authorization;
using AdminPanelProject.Dtos.Users;
using AdminPanelProject.Helper;
using AdminPanelProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using static System.Runtime.InteropServices.JavaScript.JSType;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _svc;
    private readonly IRoleService _roleService;
    private readonly IAuditLogService _auditLogService;
    private readonly IValidationService _validator;
    
    public UsersController(IUserService svc, IAuditLogService auditLogService, IValidationService validator, IRoleService roleService)
    { 
        _svc = svc;
        _auditLogService = auditLogService;
        _validator = validator;
        _roleService = roleService;
    }


    [HttpGet]
    [Route("[action]")]
    public async Task<string> count()
    {
        string no = await _svc.countAsync();
        return no;
    }



    [HttpGet]
    [HasPermission("Users.List")]
    public async Task<IActionResult> Get(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] string? email = null,
        [FromQuery] string? phone = null,
        //[FromQuery] Guid? roleId=null,
        [FromQuery] string? roleId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = "CreatedOn",
        [FromQuery] string? sortDirection = "desc"
       )
    {

        try
        {
            //if (!ModelState.IsValid)
            //{
            //    return BadRequest(new { message = "Invalid query parameter." });
            //}
            string[] allowedSort = { "createdon", "name", "email", "phone", "isactive", "role" };
            string? isActiveRaw = HttpContext.Request.Query["isActive"].ToString();
            var validation = QueryValidator.ValidateQuery(pageNumber, pageSize, sortBy, sortDirection, allowedSort, isActiveRaw);

            if(!validation.IsValid)
            {
                return BadRequest(new {message=validation.ErrorMessage});
            }
            Guid? parsedRoleId = null;

            
            var roleIdRaw = HttpContext.Request.Query["roleId"].ToString();
            if (!string.IsNullOrWhiteSpace(roleIdRaw))
            {
                if (!Guid.TryParse(roleIdRaw, out var tempGuid))
                {
                return BadRequest(new { message = "Invalid role ID." });

                }
                parsedRoleId = tempGuid;
            }


            var (items, total) = await _svc.GetPagedAsync(pageNumber, pageSize, name, email, phone, parsedRoleId, isActive, sortBy, sortDirection);

        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

        await _auditLogService.LogAsync(fullName, "View", "Viewed Users List");

        return Ok(new { totalCount = total, pageNumber, pageSize, items });
        }
        catch(Exception ex)
        {
            var c = ex.GetType().Name;
            return StatusCode(500, new { message = "Unable to fetch data from  the database", code=c });
        }
    }

    [HttpGet("{id}")]
    [HasPermission("Users.List")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {

        var d = await _svc.GetByIdAsync(id);
        if (d == null) return NotFound();

        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

            //await _auditLogService.LogAsync(fullName, "View", $"Viewed User {d.Email}");
            await _auditLogService.LogAsync(fullName, "View", $"Viewed User details");
            return Ok(d);
        }
        catch(Exception ex)
        {
            return StatusCode(500, new { message = "Unable to fetch user Data" });
        }
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    [HasPermission("Users.Add")]
    public async Task<IActionResult> Create([FromForm] CreateUserDto dto)
    {
        try
        {

            

        var fullName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

            if (!_validator.IsValidName(dto.FirstName))
                return BadRequest(new { message = "Invalid first name." });

            if (!_validator.IsValidName(dto.LastName))
                return BadRequest(new { message = "Invalid last name." });

            if (!_validator.IsValidEmail(dto.Email))
                return BadRequest(new { message = "Invalid email format." });

            if (!_validator.IsStrongPassword(dto.Password))
                return BadRequest(new { message = "Password must have uppercase, number, and special character." });

            if (!_validator.IsValidPhone(dto.PhoneNumber))
                return BadRequest(new { message = "Invalid phone number." });


            var file = Request.Form.Files.FirstOrDefault();
        var (success, error, code) = await _svc.CreateAsync(dto, file, fullName);

        if (!success)
        {
            if (code == "ProtectedRole")
                return StatusCode(403, new { message = error }); // 🚫 forbidden
            if (code == "Duplicate")
                return Conflict(new { message = error }); // ⚠️ 409 Conflict
            return BadRequest(new { message = error });
        }

        await _auditLogService.LogAsync(fullName, "Create", $"Created User {dto.Email}");
        return Ok(new { message = "User successfully added!" });
        }
        catch(Exception ex)
        {
            return StatusCode(500, new { message = "Unable to add the user" });
        }
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(10_000_000)]
    [HasPermission("Users.Edit")]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateUserDto dto)
    {
        try
        {

            if (!_validator.IsValidName(dto.FirstName))
                return BadRequest(new { message = "Invalid first name" });

            if (!_validator.IsValidName(dto.LastName))
                return BadRequest(new { message = "Invalid last name" });

            if (!_validator.IsValidPhone(dto.PhoneNumber))
                return BadRequest(new { message = "Invalid phone number" });

            // Reset password validation
            if (!string.IsNullOrWhiteSpace(dto.ResetPassword) &&
                !_validator.IsStrongPassword(dto.ResetPassword))
            {
                return BadRequest(new
                {
                    message = "Invalid password. Must be 8–50 chars, include 1 uppercase letter, 1 digit, and 1 special character."
                });
            }

            // Role check
            if (!await _roleService.RoleExistsAsync(dto.RoleId))
                return BadRequest(new { message = "Invalid role" });


            var file = Request.Form.Files.FirstOrDefault();

        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? "Unknown";
        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

        var (success, error, code) = await _svc.UpdateAsync(id, dto, file, fullName);

        if (!success)
        {
            if (code == "NotFound")
                return NotFound(new { message = error }); // 404
            if (code == "ProtectedRole")
                return StatusCode(403, new { message = error }); // 403
            if (code == "InvalidRole")
                return BadRequest(new { message = error }); // 400
            if (code == "PasswordResetFailed" || code == "UpdateFailed")
                return Conflict(new { message = error }); // 409

            return BadRequest(new { message = error }); // fallback
        }

            //await _auditLogService.LogAsync(fullName, "Update", $"Updated user {dto.FirstName} {dto.LastName}");
            await _auditLogService.LogAsync(fullName, "Update", $"User updated successfully");
            return Ok(new { message = "User updated successfully" });
        }
        catch(Exception ex)
        {
            return StatusCode(500, new { message = "Unable to update the user" });
        }
    }


    [HttpPatch("{id}/toggle")]
    [HasPermission("Users.Edit")]
    public async Task<IActionResult> Toggle(Guid id)
    {   
        var fullName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

        var (success, error) = await _svc.ToggleStatusAsync(id, fullName);
        if (!success)
            return NotFound(new { message = error });
        await _auditLogService.LogAsync(fullName, "Update", $"Updated user status ");

        return Ok(new { message = "User status updated successfully" });
    }

    [HttpDelete("{id}")]
    [HasPermission("Users.Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {

        var (success, error, code) = await _svc.DeleteAsync(id);

        var fullName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

        if (!success)
        {
            if (code == "NotFound")
                return NotFound(new { message = error });
            if (code == "ProtectedRole")
                return StatusCode(403, new { message = error });
            return Conflict(new { message = error });
        }

            //await _auditLogService.LogAsync(fullName, "Delete", $"Deleted User {id}");
            await _auditLogService.LogAsync(fullName, "Delete", $"User successfully deleted");
        return Ok(new { message = "User deleted successfully" });
        }
        catch
        {
            return StatusCode(500, new { message = "Unable to delete the user" });
        }
    }
    [HttpPost("{id}/change-password")]
    [HasPermission("Users.Edit")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto)
    {
        try
        {
            if (dto.oldPassword == null || dto.newPassword == null)
                return BadRequest(new { message = "Both current and new password are required." });

            if (!_validator.IsStrongPassword(dto.oldPassword) || !_validator.IsStrongPassword(dto.newPassword))
            {
                return BadRequest(new
                {
                    message = "Invalid password. Must be 8–50 chars, include 1 uppercase letter, 1 digit, and 1 special character."
                });
            }

            var (success, error, code) = await _svc.ChangePasswordAsync(id, dto.oldPassword, dto.newPassword);

            if (!success)
            {
                if (code == "NotFound")
                    return NotFound(new { message = error });
                if (code == "InvalidPassword")
                    return StatusCode(403, new { message = error, code = "InvalidPassword" });
                if (code == "ChangeFailed")
                    return Conflict(new { message = error });
            }

            // ✅ Audit Log
            var fullName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = User.Identity?.Name ?? "Unknown";

            await _auditLogService.LogAsync(fullName, "Update", $"Changed its own password.");

            return Ok(new { message = "Password changed successfully!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unable to change password" });
        }
    }


    [HttpGet("export")]
    [HasPermission("Users.List")]
    public async Task<IActionResult> Export()
    {
        try
        {
            var firstName = User.FindFirst("FirstName")?.Value;
            var lastName = User.FindFirst("LastName")?.Value;
            var fullName = $"{firstName} {lastName}".Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                fullName = User.Identity?.Name ?? "Unknown";

            await _auditLogService.LogAsync(fullName, "Export", "Exported Users List CSV");

            // 🔹 Only log the action — no data returned
            return Ok(new { message = "Audit logged for user export CSV" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to log user export", error = ex.Message });
        }
    }


    [HttpGet("~/api/roles/simple")]
    //[HasPermission("Users.edit", "Users.Add")]
    [HasPermission("Users.List")]
    public async Task<IActionResult> RolesSimple([FromServices] IUserService svc)
    {
        var list = await svc.GetRolesSimpleAsync();
        return Ok(list.Select(x => new { id = x.Id, name = x.Name }));
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        if (email.Contains(" ")) return false;
        // must contain word before and after '@'
        var parts = email.Split('@');
        if (parts.Length != 2) return false;
        if (string.IsNullOrWhiteSpace(parts[0])) return false; // before @
        if (string.IsNullOrWhiteSpace(parts[1])) return false; // after @

        return true;
    }
    private bool IsStrongPassword(string password)
    {
        if (password.Length < 8 || password.Length > 50)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasDigit && hasSpecial;
    }


}
