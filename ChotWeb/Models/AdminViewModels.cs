using System.ComponentModel.DataAnnotations;

namespace ChotWeb.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your username.")]
        [StringLength(100)]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Enter your password.")]
        [StringLength(200)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Keep me signed in on this device")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
