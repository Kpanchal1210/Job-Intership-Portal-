using System.ComponentModel.DataAnnotations;

namespace Job_InternshipPortal.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(50, ErrorMessage = "Full Name cannot exceed 50 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // ── Security: model-layer allowlist prevents any role value other than
        //    JobSeeker or Recruiter from passing server-side model validation.
        //    The controller adds a second explicit check before role assignment.
        [Required(ErrorMessage = "Please select your account type.")]
        [Display(Name = "I am a")]
        [AllowedValues(UserRoles.JobSeeker, UserRoles.Recruiter,
            ErrorMessage = "Invalid account type selected.")]
        public string Role { get; set; } = UserRoles.JobSeeker;
    }
}
