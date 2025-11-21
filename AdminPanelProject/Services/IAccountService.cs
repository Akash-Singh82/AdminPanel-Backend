using AdminPanelProject.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace AdminPanelProject.Services
{
    public interface IAccountService
    {
        Task<IdentityResult> RegisterUserAsync(RegisterViewModel model);
        Task<IdentityResult> ConfirmEmailAsync(Guid userId, string token);
        Task<SignInResult> LoginUserAsync(LoginViewModel model);

        //Task<(SignInResult , bool Resent)> LoginUserAsync(LoginViewModel model);
        Task LogoutUserAsync();
        Task SendEmailConfirmationAsync(string email);
        Task<ProfileViewModel> GetUserProfileByEmailAsync(string email);




        //New Methods for Forgot Password
        Task<bool> SendPasswordResetLinkAsync(string email);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordViewModel model);
    }
}
