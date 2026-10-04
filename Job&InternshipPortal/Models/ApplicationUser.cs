using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JobInternshipPortal.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FullName { get; set; } = string.Empty;

        public Company? Company { get; set; }
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    }
}
