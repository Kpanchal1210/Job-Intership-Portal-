using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Job_InternshipPortal.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(3000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Requirements { get; set; }

        [Required]
        public JobType JobType { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Salary { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastDate { get; set; }

        [Required]
        public JobStatus Status { get; set; } = JobStatus.Pending;

        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    }
}
