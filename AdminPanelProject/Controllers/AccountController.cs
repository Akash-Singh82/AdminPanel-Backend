using AdminPanelProject.Models;
using AdminPanelProject.Services;
using AdminPanelProject.ViewModels;
using ASPNETCoreIdentityDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ASPNETCoreIdentityDemo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class accountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<accountController> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IReCaptchaService _reCaptchaService;

        private readonly UserManager<ApplicationUser> _userManager; 
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        public accountController(IAccountService accountService, 
                                 ILogger<accountController> logger, 
                                 UserManager<ApplicationUser> userManager, 
                                 IConfiguration configuration,
                                 IPermissionService permissionService,
                                 IReCaptchaService reCaptchaService,
                                 IAuditLogService auditLogService) 

        { 
            _accountService = accountService; 
            _logger = logger; 
            _userManager = userManager;
            _configuration = configuration;
            _permissionService = permissionService;
            _reCaptchaService = reCaptchaService;
            _auditLogService = auditLogService;
        }



        // POST: api/Account/Register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // ✅ 2️⃣ ADD THIS: server-side email uniqueness check
                // This prevents duplicate users even if two requests race at the same time.
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    // Add model-level validation error
                    ModelState.AddModelError("Email", "Email is already in use.");
                    return BadRequest(ModelState);
                }

                var result = await _accountService.RegisterUserAsync(model);
                if (result.Succeeded)
                    return Ok(new { message = "Registration successful. Please check your email to confirm." });

                return BadRequest(result.Errors.Select(e => e.Description));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", model.Email);
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        // GET: api/Account/ConfirmEmail?userId={userId}&token={token}
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
        {
            try
            {
                var result = await _accountService.ConfirmEmailAsync(userId, token);
                if (result.Succeeded)
                    return Ok(new { message = "Email confirmed successfully." });

                return BadRequest(result.Errors.Select(e => e.Description));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for UserId: {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred during email confirmation.");
            }
        }

        // POST: api/Account/Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);



            try
            {
                var result = await _accountService.LoginUserAsync(model);
                if (result.Succeeded)
                {
                   
                    var user = await _userManager.FindByEmailAsync(model.Email); 
                   var jwtSettings = _configuration.GetSection("JwtSettings"); 
                    var secret = jwtSettings["SecretKey"]; 
                    var issuer = jwtSettings["Issuer"]; 
                var audience = jwtSettings["Audience"]; 
                var expiryMinutes = int.TryParse(jwtSettings["ExpiryMinutes"], out var m) ? m : 60;

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)); 
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim> 
                { 
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), 
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty), 
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new Claim("FirstName", user.FirstName ?? string.Empty),
                    new Claim("LastName", user.LastName ?? string.Empty)
                };

                    var firstName = user.FirstName;
                    var lastName = user.LastName;
                    var fullName = $"{firstName} {lastName}".Trim();

                    if (string.IsNullOrWhiteSpace(fullName))
                        fullName = User.Identity?.Name ?? "Unknown";

                    await _auditLogService.LogAsync(fullName, "Authentication", "Login");


                    // add role claims
                    var roles = await _userManager.GetRolesAsync(user); 
                foreach (var r in roles) 
                     claims.Add(new Claim(ClaimTypes.Role, r));


                    var permissions = await _permissionService.GetPermissionsByUserIdAsync(Guid.Parse(user.Id.ToString()));
                    foreach (var p in permissions)
                    {
                        // Use a custom claim type "Permission"
                        claims.Add(new Claim("Permission", p.Name));
                    }

                    var token = new JwtSecurityToken( 
                    issuer: issuer, 
                    audience: audience, 
                    claims: claims, 
                    expires: DateTime.UtcNow.AddMinutes(expiryMinutes), 
                    signingCredentials: creds 
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token); 
                return Ok(new { token = tokenString, message = "Login successful" });

                

                }
                if (result.IsNotAllowed)
                {  
                    var message ="Email is not confirmed yet.";

           
                    return Unauthorized(new { message  });
                }

                return Unauthorized(new { message = "Invalid Credentials or User not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", model.Email);
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }

           
        }

       

        // GET: api/Account/Profile
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "User is not logged in" });

            try
            {
                var model = await _accountService.GetUserProfileByEmailAsync(email);
                if (!string.IsNullOrWhiteSpace(model.ImageUrl))
                {
                    // If it's already absolute, leave it
                    if (!Uri.IsWellFormedUriString(model.ImageUrl, UriKind.Absolute))
                    {
                        var request = HttpContext.Request;
                        var baseUrl = $"{request.Scheme}://{request.Host.Value}";
                        model.ImageUrl = $"{baseUrl}{model.ImageUrl}";
                    }
                }
                else
                {
                    model.ImageUrl = null;
                }
                return Ok(model);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: api/Account/Logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var firstName = User.FindFirst("FirstName")?.Value;
                var lastName = User.FindFirst("LastName")?.Value;
                var fullName = $"{firstName} {lastName}".Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = User.Identity?.Name ?? "Unknown";

                await _auditLogService.LogAsync(fullName, "Authentication", "Logged out");

                await _accountService.LogoutUserAsync();
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, "Error occurred during logout.");
            }
        }
       

        // POST: api/Account/ResendEmailConfirmation
        [HttpPost("resend-email-confirmation")]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendConfirmationEmailViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _accountService.SendEmailConfirmationAsync(model.Email);
                return Ok(new { message = "If the email is registered, a confirmation link has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending email confirmation to: {Email}", model.Email);
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }



        // POST: api/account/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Do not reveal user existence
            var result = await _accountService.SendPasswordResetLinkAsync(model.Email);

            // Always return success (to avoid user enumeration)
            return Ok(new { message = "If an account with that email exists, you will receive a password reset email." });
        }

        // POST: api/account/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _accountService.ResetPasswordAsync(model);
            if (!result.Succeeded)
            {
                // collect errors
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(new { errors });
            }

            return Ok(new { message = "Password has been reset successfully." });
        }

        // GET: api/account/health
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }

    }
}
