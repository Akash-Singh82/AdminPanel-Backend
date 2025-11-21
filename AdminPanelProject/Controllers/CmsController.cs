// Controllers/CmsController.cs
using AdminPanelProject.Authorization;
using AdminPanelProject.Dtos.Cms;
using AdminPanelProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CmsController : ControllerBase
{
    private readonly ICmsService _svc;
    private readonly IAuditLogService _auditLogService;

    public CmsController(ICmsService svc, IAuditLogService auditLogService)
    {
        _svc = svc;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [HasPermission("Cms.List")]
    public async Task<IActionResult> Get([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? title = null, [FromQuery] string? key = null, [FromQuery] string? metaKeyword = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortField="createdon", [FromQuery] string? sortDirection="asc" )
    {
        try
        {
            string[] allowedSort = { "createdon", "title", "key", "metakeyword", "isactive" };
            string? isActiveRaw = HttpContext.Request.Query["isActive"].ToString();
            sortField = sortField?.Trim().ToLower();
            sortDirection = sortDirection?.Trim().ToLower();
            var validation = QueryValidator.ValidateQuery(pageNumber, pageSize, sortField, sortDirection, allowedSort, isActiveRaw);

            if (!validation.IsValid)
            {
                return BadRequest(new { message = validation.ErrorMessage });
            }

            var (items, total) = await _svc.GetPagedAsync(pageNumber, pageSize, title, key, metaKeyword, isActive, sortField, sortDirection);

        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

        await _auditLogService.LogAsync(fullName, "View", "Viewed CMS list");

        return Ok(new { totalCount = total, pageNumber, pageSize, items });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "unable to get the cms list" });
        }
    }

    [HttpGet("{id}")]
    [HasPermission("Cms.List")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {

        var d = await _svc.GetByIdAsync(id);
        if (d == null) return NotFound(new {message="Cms not found"});

        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

        await _auditLogService.LogAsync(fullName, "View", $"Viewed CMS {d.Key}");
        return Ok(d);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "unable to get the cms detail" });
        }
    }



    [HttpPost]
    [HasPermission("Cms.Add")]
    public async Task<IActionResult> Create([FromBody] CreateCmsDto dto)
    {
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? "Unknown";
        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName)) fullName = User.Identity?.Name ?? "Unknown";
        try
        {
            var keyValidation = ValidateKey(dto.Key);
            if (!keyValidation.IsValid)
                return BadRequest(new { message = keyValidation.ErrorMessage });

           
            var titleValidation = ValidateTitleOrSubject(dto.Title, "Title");
            if (!titleValidation.IsValid)
                return BadRequest(new { message = titleValidation.ErrorMessage });

            var metaKeywordValidation = ValidateTitleOrSubject(dto.MetaKeyword, "MetaKeyword");
            if (!metaKeywordValidation.IsValid)
                return BadRequest(new { message = metaKeywordValidation.ErrorMessage });

            var metaTitle = ValidateTitleOrSubject(dto.MetaTitle, "MetaTitle");
            if (!metaTitle.IsValid)
                return BadRequest(new { message = metaTitle.ErrorMessage });

            var metaDescription = ValidateTitleOrSubject(dto.MetaDescription, "MetaDescription");
            if (!metaDescription.IsValid)
                return BadRequest(new { message = metaDescription.ErrorMessage });
            if (dto.Content.Length < 2 || dto.Content.Length > 100)
                return BadRequest(new { message = "body must be 2 and 100 characters" });




            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Invalid data", errors });
            }
            var (success, error, code) = await _svc.CreateAsync(dto, fullName);

            if (!success)
            {
                // Handle specific error codes from service
                if (code == "Duplicate")
                    return Conflict(new { message = error }); // HTTP 409

                if (code == "DbError")
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = error });

                // Default fallback
                return BadRequest(new { message = error ?? "Failed to create CMS page." });
            }

            await _auditLogService.LogAsync(fullName, "Create", $"Created CMS Successfully");
            return Ok(new
            {
                success = true,
                message = "CMS created successfully."
            });


        }
        catch (DbUpdateException ex)
        {
            //_logger.LogError(ex, "Database error while creating CMS");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "A database error occurred while creating the CMS page.",
                errorCode = "DbError",
                details = ex.InnerException?.Message ?? ex.Message
            });
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Unexpected error while creating CMS");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while creating the CMS page.",
                errorCode = "Unexpected",
                details = ex.Message
            });
        }

    }




    [HttpPut("{id}")]
    [HasPermission("Cms.Edit")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCmsDto dto)
    {
        try
        {

            var titleValidation = ValidateTitleOrSubject(dto.Title, "Title");
            if (!titleValidation.IsValid)
                return BadRequest(new { message = titleValidation.ErrorMessage });

            var metaKeywordValidation = ValidateTitleOrSubject(dto.MetaKeyword, "MetaKeyword");
            if (!metaKeywordValidation.IsValid)
                return BadRequest(new { message = metaKeywordValidation.ErrorMessage });

            var metaTitle = ValidateTitleOrSubject(dto.MetaTitle, "MetaTitle");
            if (!metaTitle.IsValid)
                return BadRequest(new { message = metaTitle.ErrorMessage });

            var metaDescription = ValidateTitleOrSubject(dto.MetaDescription, "MetaDescription");
            if (!metaDescription.IsValid)
                return BadRequest(new { message = metaDescription.ErrorMessage });
            if (dto.Content.Length < 2 || dto.Content.Length > 100)
                return BadRequest(new { message = "body must be 2 and 100 characters" });


            var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName)) fullName = User.Identity?.Name ?? "Unknown";

        var ok = await _svc.UpdateAsync(id, dto, fullName);
        await _auditLogService.LogAsync(fullName, "Update", $"Updated CMS Successfully");
        return ok ? Ok() : BadRequest("Update failed");
        }
        catch (DbUpdateException ex)
        {
            return BadRequest(ex.Message);
        }
        catch(Exception ex)
        {
            return StatusCode(500, new { message = " Unable to edit the cms" });
        }
    }

    // for toggle only
    [HttpPut("{id}/toggle-status")]
    [HasPermission("Cms.Edit")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name ?? "Unknown";

        var ok = await _svc.ToggleStatusAsync(id, fullName);
        await _auditLogService.LogAsync(fullName, "Update", $"Updated CMS status ");
        return ok ? Ok() : NotFound(new {message="not found"});
    }

    [HttpDelete("{id}")]
    [HasPermission("Cms.Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName)) fullName = User.Identity?.Name ?? "Unknown";

        await _auditLogService.LogAsync(fullName, "Delete", $"Deleted CMS successfully");
        return ok ? Ok() : NotFound();
    }

    //[HttpGet("export")]
    //[HasPermission("Cms.List")]
    //public async Task<IActionResult> Export([FromQuery] string? title = null, [FromQuery] string? key = null,
    //    [FromQuery] string? metaKeyword = null, [FromQuery] bool? isActive = null)
    //{
    //    var fileResult = await _svc.ExportCsvAsync(title, key, metaKeyword, isActive);

    //    var firstName = User.FindFirst("FirstName")?.Value;
    //    var lastName = User.FindFirst("LastName")?.Value;
    //    var fullName = $"{firstName} {lastName}".Trim();
    //    if (string.IsNullOrWhiteSpace(fullName)) fullName = User.Identity?.Name ?? "Unknown";

    //    await _auditLogService.LogAsync(fullName, "Export", "Exported CMS CSV");
    //    return fileResult;
    //}

    [HttpGet("export")]
    [HasPermission("Cms.List")]
    public async Task<IActionResult> Export()
    {
        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName)) fullName = User.Identity?.Name ?? "Unknown";

        await _auditLogService.LogAsync(fullName, "Export", $"Exported CMS CSV");
        return Ok();
    }


    private (bool IsValid, string ErrorMessage) ValidateKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return (false, "Invalid Key");

        if (key.Length < 2 || key.Length > 50)
            return (false, "Key must be between 2 and 50 characters.");

        var regex = new Regex(@"^[A-Za-z0-9_-]+$");
        if (!regex.IsMatch(key))
            return (false, "Key can only contain letters, numbers, underscores '_' and hyphens '-' with no spaces.");

        return (true, string.Empty);
    }

    private (bool IsValid, string ErrorMessage) ValidateTitleOrSubject(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return (false, $"{fieldName} is empty");

        if (value.Length < 2 || value.Length > 50)
            return (false, $"{fieldName} must be between 2 and 50 characters.");
        var regex = new Regex(@"^[A-Za-z]+( [A-Za-z]+)*$");

        if (!regex.IsMatch(value))
            return (false, $"{fieldName} can only contain letters and single spaces between words, no leading/trailing spaces.");

        return (true, string.Empty);
    }

}
