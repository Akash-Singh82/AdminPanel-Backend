using AdminPanelProject.Models;
using AdminPanelProject.Services;
using AdminPanelProject.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace ASPNETCoreIdentityDemo.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountService(UserManager<ApplicationUser> userManager,
                              SignInManager<ApplicationUser> signInManager,
                              IEmailService emailService,
                              IConfiguration configuration,
                              IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IdentityResult> RegisterUserAsync(RegisterViewModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                DateOfBirth = model.DateOfBirth,
                IsActive = true,
                PhoneNumber = model.PhoneNumber,
                CreatedOn = DateTime.UtcNow
            };

            IdentityResult result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return result;

            // Assign "User" role by default
            IdentityResult roleAssignResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleAssignResult.Succeeded)
            {
                // Handle error - optionally return this failure instead
                // or log the issue and continue
                return roleAssignResult;
            }

            var token = await GenerateEmailConfirmationTokenAsync(user);

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? throw new InvalidOperationException("BaseUrl is not configured.");
            var confirmationLink = $"{baseUrl}/confirm-email?userId={user.Id}&token={token}";

            await _emailService.SendRegistrationConfirmationEmailAsync(user.Email, user.FirstName, confirmationLink);

            return result;
        }

        public async Task<IdentityResult> ConfirmEmailAsync(Guid userId, string token)
        {
            if (userId == Guid.Empty || string.IsNullOrEmpty(token))
                return IdentityResult.Failed(new IdentityError { Description = "Invalid token or user ID." });

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            var decodedBytes = WebEncoders.Base64UrlDecode(token);
            var decodedToken = Encoding.UTF8.GetString(decodedBytes);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
            {
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? throw new InvalidOperationException("BaseUrl is not configured.");
                var loginLink = $"{baseUrl}/login";

                await _emailService.SendAccountCreatedEmailAsync(user.Email!, user.FirstName!, loginLink);
            }

            return result;
        }

        public async Task<SignInResult> LoginUserAsync(LoginViewModel model)

        {

            var user = await _userManager.FindByEmailAsync(model.Email);



            if (user == null)

                return SignInResult.Failed;



            //if (!await _userManager.IsEmailConfirmedAsync(user))
            //{

            //    return SignInResult.NotAllowed;

            //}



            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);



            if (result.Succeeded)

            {

                // Update LastLogin 

                user.LastLogin = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);

            }



            return result;

        }

        public async Task LogoutUserAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task SendEmailConfirmationAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Prevent user enumeration by not disclosing existence
                return;
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                // Email already confirmed; no action needed
                return;
            }

            var token = await GenerateEmailConfirmationTokenAsync(user);

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? throw new InvalidOperationException("BaseUrl is not configured.");
            var confirmationLink = $"{baseUrl}/confirm-email?userId={user.Id}&token={token}";

            await _emailService.SendResendConfirmationEmailAsync(user.Email!, user.FirstName!, confirmationLink);
        }

        public async Task<ProfileViewModel> GetUserProfileByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                throw new ArgumentException("User not found.", nameof(email));

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();

            return new ProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                LastLoggedIn = user.LastLogin,
                CreatedOn = user.CreatedOn,
                DateOfBirth = user.DateOfBirth,

                ImageUrl = string.IsNullOrEmpty(user.ProfileImagePath)
            ? null
            : $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}{user.ProfileImagePath}",

               RoleName = roleName
            };
        }

        //Helper Method
        private async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            return encodedToken;
        }


        public async Task<bool> SendPasswordResetLinkAsync(string email)
        {
            // Try to find the user by their email address
            var user = await _userManager.FindByEmailAsync(email);

            // Security measure: 
            // Do not reveal whether the user exists or not — 
            // always behave the same if the user is not found or the email is not confirmed
            if (user == null)
                return false;

            // Generate a unique, secure token for password reset
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Encode the token so it can be safely used in a URL
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Construct the password reset link with the encoded token and user’s email
            var baseUrl = _configuration["AppSettings:BaseUrl"];
            var resetLink = $"{baseUrl}/account/reset-password?email={user.Email}&token={encodedToken}";

            // Send the reset link via email to the user
            await _emailService.SendPasswordResetEmailAsync(user.Email!, user.FirstName, resetLink);

            return true;
        }

        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            // Find the user associated with the provided email
            var user = await _userManager.FindByEmailAsync(model.Email);

            // If user not found, return a generic failure (no details leaked for security)
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "Invalid request." });

            // Decode the token that was passed in from the reset link
            var decodedBytes = WebEncoders.Base64UrlDecode(model.Token);
            var decodedToken = Encoding.UTF8.GetString(decodedBytes);

            // Attempt to reset the user's password with the new one
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

            // If successful, update the Security Stamp to invalidate any active sessions or tokens
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);

                // send notification email: password changed
                try
                {
                    var baseUrl = _configuration["AppSettings:BaseUrl"] ?? string.Empty;
                    var loginLink = $"{baseUrl}/login";
                    await _emailService.SendPasswordChangedConfirmationEmailAsync(user.Email!, user.FirstName!, loginLink);
                }
                catch
                {
                    // optionally log email failure but don't fail the whole operation
                }
            }

            return result;
        }
    }
}