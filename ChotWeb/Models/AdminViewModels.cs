using System.ComponentModel.DataAnnotations;

namespace ChotWeb.Models
{
    /// <summary>Sign-in step 1: the admin types their email.</summary>
    public class LoginEmailViewModel
    {
        [Required(ErrorMessage = "Enter your email.")]
        [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
        [StringLength(254)]
        public string Email { get; set; } = "";

        public string? ReturnUrl { get; set; }
    }

    /// <summary>Sign-in step 2: the admin types the 6-digit code that was emailed to them.</summary>
    public class LoginCodeViewModel
    {
        [Required]
        [StringLength(254)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Enter the 6-digit code from the email.")]
        [StringLength(12)]
        [Display(Name = "6-digit code")]
        public string Code { get; set; } = "";

        [Display(Name = "Keep me signed in on this device")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
