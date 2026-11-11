using System.ComponentModel.DataAnnotations;

namespace ChotWeb.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your username.")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Keep me signed in on this device")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class SetupViewModel
    {
        [Required(ErrorMessage = "Enter the setup code.")]
        [Display(Name = "Setup code")]
        public string SetupCode { get; set; } = "";

        [Required(ErrorMessage = "Choose a username.")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "Username must be 3–40 characters.")]
        [RegularExpression(@"^[A-Za-z0-9._@\-]+$", ErrorMessage = "Use letters, numbers and . _ @ - only.")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Choose a password.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Type the password again.")]
        [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class AccountViewModel
    {
        [Required(ErrorMessage = "Enter a username.")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "Username must be 3–40 characters.")]
        [RegularExpression(@"^[A-Za-z0-9._@\-]+$", ErrorMessage = "Use letters, numbers and . _ @ - only.")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Enter your current password to save changes.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = "";

        [StringLength(128, MinimumLength = 8, ErrorMessage = "New password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }

        [Compare(nameof(NewPassword), ErrorMessage = "The new passwords don't match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string? ConfirmNewPassword { get; set; }
    }
}
