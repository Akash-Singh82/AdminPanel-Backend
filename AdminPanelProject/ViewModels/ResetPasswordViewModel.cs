using System.ComponentModel.DataAnnotations;

namespace AdminPanelProject.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid Email address.")]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password, ErrorMessage = "Invalid Password format.")]
        public string Password { get; set; } = null!;
        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password, ErrorMessage = "Invalid Password format.")]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password must match.")]
        public string ConfirmPassword { get; set; } = null!;
        [Required(ErrorMessage = "The password reset token is required.")]
        public string Token { get; set; } = null!;
    }
}
