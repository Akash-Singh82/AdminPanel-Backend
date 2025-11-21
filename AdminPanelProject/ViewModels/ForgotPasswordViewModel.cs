using System.ComponentModel.DataAnnotations;

namespace AdminPanelProject.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your Email address.")]
        [EmailAddress(ErrorMessage = "The Email address is not valid.")]
        public string Email { get; set; } = null!;
    }
}
