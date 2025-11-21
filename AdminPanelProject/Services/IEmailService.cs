
namespace AdminPanelProject.Services
{
    public interface IEmailService 
    {
        Task SendRegistrationConfirmationEmailAsync(string toEmail, string firstName, string confirmationLink);
        Task SendAccountCreatedEmailAsync(string toEmail, string firstName, string loginLink);
        Task SendResendConfirmationEmailAsync(string toEmail, string firstName, string confirmationLink);

        //New Method to send Password Reset Link
        Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink);
        Task SendPasswordChangedConfirmationEmailAsync(string toEmail, string firstName, string loginLink);
    }
}
