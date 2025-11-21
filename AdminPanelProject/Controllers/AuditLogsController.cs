using AdminPanelProject.Authorization;
using AdminPanelProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _svc;
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService svc, IAuditLogService auditLogService)
        {
            _svc = svc;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [HasPermission("AuditLogs.List")]
        public async Task<IActionResult> Get(
            [FromQuery] string? userName,
            [FromQuery] string? type,
            [FromQuery] string? activity,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortField = "Timestamp",
            [FromQuery] string? sortDirection = "desc")
        {
            try
            {

                string[] allowedSort = { "createdon", "username", "type", "activity", "timestamp" };
                string? isActiveRaw = "true";
                sortField = sortField?.Trim().ToLower();
                sortDirection = sortDirection?.Trim();
                var validation = QueryValidator.ValidateQuery(pageNumber, pageSize, sortField, sortDirection, allowedSort, isActiveRaw);

                if (!validation.IsValid)
                {
                    return BadRequest(new { message = validation.ErrorMessage });
                }
                userName = userName?.Trim();
                type = type?.Trim();
                activity = activity?.Trim();
                sortField = sortField?.Trim();
                sortDirection = sortDirection?.Trim();


                var (logs, total) = await _svc.GetPagedAsync(userName, type, activity, fromDate, toDate, pageNumber, pageSize, sortField, sortDirection);

                var firstName = User.FindFirst("FirstName")?.Value;
                var lastName = User.FindFirst("LastName")?.Value;
                var fullName = $"{firstName} {lastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = User.Identity?.Name ?? "Unknown";

                await _auditLogService.LogAsync(fullName, "View", $"Viewed Audit list");
                return Ok(new { data = logs, total });
            }
            catch(ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }catch(Exception ex)
            {
                return StatusCode(500, new {message = "An error occurred while retrieving audit logs.", details=ex.Message});
            }
        }

        [HttpGet("export")]
        [HasPermission("AuditLogs.List")]
        public async Task<IActionResult> Export()
        {
            var firstName = User.FindFirst("FirstName")?.Value;
            var lastName = User.FindFirst("LastName")?.Value;
            var fullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = User.Identity?.Name ?? "Unknown";

            await _auditLogService.LogAsync(fullName, "Export", $"Exported Audit CSV");
            return Ok();
        }


    }
}
