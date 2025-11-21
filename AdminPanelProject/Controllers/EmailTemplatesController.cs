using AdminPanelProject.Authorization;
using AdminPanelProject.Services;
using AdminPanelProject.ViewModels;
using AdminPanelProject.ViewModels.EmailTemplate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AdminPanelProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmailTemplatesController : Controller
    {
        private readonly IEmailTemplateService _svc;
        private readonly IWebHostEnvironment _env;
        private readonly IAuditLogService _audit;
        private readonly IValidationService _validator;

        public EmailTemplatesController(IEmailTemplateService svc, IWebHostEnvironment env, IAuditLogService audit, IValidationService validator)
        {
            _svc = svc;
            _env = env;
            _audit = audit;
            _validator = validator;
        }

        private string GetUserFullName()
        {
            var firstName = User.FindFirst("FirstName")?.Value;
            var lastName = User.FindFirst("LastName")?.Value;
            var fullName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = User.Identity?.Name ?? "Unknown";
            return fullName;
        }


        [HttpGet]
        [HasPermission("EmailTemplates.List")]
        public async Task<ActionResult<PagedResult<EmailTemplateListItemDto>>> Get(
           [FromQuery] int pageNumber = 1,
           [FromQuery] int pageSize = 10,
           [FromQuery] string? key = null,
           [FromQuery] string? title = null,
           [FromQuery] string? subject = null,
           [FromQuery] bool? isActive = null,
           [FromQuery] string sortField="key",
           [FromQuery] string sortDirection="asc"
           
            )
        {
            try
            {
                string[] allowedSort = { "key", "title", "subject","isactive", "createdon" };
                string? isActiveRaw = HttpContext.Request.Query["isActive"].ToString();
                var validation = QueryValidator.ValidateQuery(pageNumber, pageSize, sortField, sortDirection, allowedSort, isActiveRaw);

                if (!validation.IsValid)
                {
                    return BadRequest(new { message = validation.ErrorMessage });
                }



                var result = await _svc.GetPagedAsync(pageNumber, pageSize, key, title, subject, isActive, sortField, sortDirection);

            var user = GetUserFullName();
                //await _audit.LogAsync(user, "View", $"Viewed Email Template list (Page: {pageNumber}, Size: {pageSize})");
                await _audit.LogAsync(user, "View", $"Viewed Email Template list");

                return Ok(result);
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = "Issue in server Unable to fetch the data" });
            }
        }

        [HttpGet("{id}")]
        [HasPermission("EmailTemplates.List")]
        public async Task<ActionResult<EmailTemplateDetailsDto>> GetById(Guid id)
        {
            var user = GetUserFullName();

            var dto = await _svc.GetByIdAsync(id);
            if (dto == null) return NotFound(new {message="EmailTemplate not found"});

            await _audit.LogAsync(user, "View", $"Viewed Email Template details ");
            return Ok(dto);
        }

        [HttpPost]
        [HasPermission("EmailTemplates.Add")]
        public async Task<IActionResult> Create([FromBody] EmailTemplateCreateDto dto)
        {

            var keyValidation = ValidateKey(dto.Key);
            if (!keyValidation.IsValid)
                return BadRequest(new { message = keyValidation.ErrorMessage });

            // 3️⃣ Validate title and subject
            var titleValidation = ValidateTitleOrSubject(dto.Title, "Title");
            if (!titleValidation.IsValid)
                return BadRequest(new { message = titleValidation.ErrorMessage });

            var subjectValidation = ValidateTitleOrSubject(dto.Subject, "Subject");
            if (!subjectValidation.IsValid)
                return BadRequest(new { message = subjectValidation.ErrorMessage });

            var fromNameValidation = ValidateTitleOrSubject(dto.FromName, "FromName");
            if (!fromNameValidation.IsValid)
                return BadRequest(new { message = fromNameValidation.ErrorMessage });

            if (!_validator.IsValidEmail(dto.FromEmail))
                return BadRequest(new { message = "Invalid email format." });


            if (dto.Body.Length < 2 || dto.Body.Length > 100)
                return BadRequest(new { message = "body must be 2 and 100 characters" });

            var user = GetUserFullName();
            var (success, error, code, result) = await _svc.CreateAsync(dto, user);

            if (!success)
            {
                if (code == "Duplicate")
                    return Conflict(new { message = error }); // 409
                if (code == "DbError")
                    return StatusCode(500, new { message = error });
                return BadRequest(new { message = error }); // default 400
            }

            await _audit.LogAsync(user, "Create", $"Created Email Template Successfully");
            return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
        }


        [HttpPut("{id}")]
        [HasPermission("EmailTemplates.Edit")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EmailTemplateEditDto dto)
        {
            if (id != dto.Id) return BadRequest(new { message = "ID mismatch." });

            var titleValidation = ValidateTitleOrSubject(dto.Title, "Title");
            if (!titleValidation.IsValid)
                return BadRequest(new { message = titleValidation.ErrorMessage });

            var subjectValidation = ValidateTitleOrSubject(dto.Subject, "Subject");
            if (!subjectValidation.IsValid)
                return BadRequest(new { message = subjectValidation.ErrorMessage });

            var fromNameValidation = ValidateTitleOrSubject(dto.FromName, "FromName");
            if (!fromNameValidation.IsValid)
                return BadRequest(new { message = fromNameValidation.ErrorMessage });

            if (!_validator.IsValidEmail(dto.FromEmail))
                return BadRequest(new { message = "Invalid email format." });


            if (dto.Body.Length < 2 || dto.Body.Length > 100)
                return BadRequest(new { message = "body must be 2 and 100 characters" });



            var user = GetUserFullName();
            var (success, error, code, result) = await _svc.UpdateAsync(dto, user);

            if (!success)
            {
                if (code == "NotFound")
                    return NotFound(new { message = error });
                if (code == "Duplicate")
                    return Conflict(new { message = error });
                if (code == "DbError")
                    return StatusCode(500, new { message = error });
                return BadRequest(new { message = error });
            }

            await _audit.LogAsync(user, "Update", $"Updated Email Template Successfully");
            return Ok(result);
        }

        [HttpPut("{id}/toggle-status")]
        [HasPermission("EmailTemplates.Edit")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var user = GetUserFullName();


            var (ok, message) = await _svc.ToggleStatusAsync(id, user);
            if (!ok)
                return NotFound(new {message=message});

            await _audit.LogAsync(user, "Toggle", $"Updated Email Template status ");
            return Ok();
        }




        [HttpDelete("{id}")]
        [HasPermission("EmailTemplates.Delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Invalid or empty Id" });
            var user = GetUserFullName();
            var (success, error, code) = await _svc.DeleteAsync(id);

            if (!success)
            {
                if (code == "NotFound")
                    return NotFound(new { message = error });
                if (code == "DbError")
                    return StatusCode(500, new { message = error });
                return BadRequest(new { message = error });
            }

            await _audit.LogAsync(user, "Delete", $"Deleted Email Template");
            return NoContent();
        }




        [HttpPost("upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
        [HasPermission("EmailTemplates.Add", "EmailTemplates.Edit")]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload == null || upload.Length == 0) return BadRequest("No file uploaded.");

            var allowed = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
            var ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest("Unsupported file type.");

            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "emailtemplates");
            if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await upload.CopyToAsync(stream);
            }

            // build URL relative to web root
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var url = $"{baseUrl}/uploads/emailtemplates/{fileName}";

            // CKEditor 5 expects { "url": "..." } on simple upload adapter
            return Ok(new { url });
        }


        [HttpGet("export")]
        [HasPermission("EmailTemplates.List")]
        public async Task<IActionResult> Export()
        {
            try
            {
                var firstName = User.FindFirst("FirstName")?.Value;
                var lastName = User.FindFirst("LastName")?.Value;
                var fullName = $"{firstName} {lastName}".Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = User.Identity?.Name ?? "Unknown";

                await _audit.LogAsync(fullName, "Export", "Exported Email-Templates CSV");

                // 🔹 Only log the action — no data returned
                return Ok(new { message = "Audit logged for user export CSV" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to log user export", error = ex.Message });
            }
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
}
