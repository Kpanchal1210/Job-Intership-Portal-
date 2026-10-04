using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Job_InternshipPortal.Models
{
    public class SavedJob
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicantId { get; set; } = string.Empty;

        [ForeignKey("ApplicantId")]
        public ApplicationUser? Applicant { get; set; }

        public int JobId { get; set; }

        [ForeignKey("JobId")]
        public Job? Job { get; set; }

        [Required]
        public DateTime SavedDate { get; set; } = DateTime.UtcNow;
    }
}
