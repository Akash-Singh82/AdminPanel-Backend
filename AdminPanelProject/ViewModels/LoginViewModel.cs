using System.ComponentModel.DataAnnotations;

namespace AdminPanelProject.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email Id is Required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password Id is Required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }

        //public string ReCaptchaToken { get; set; } = string.Empty;

    }
}