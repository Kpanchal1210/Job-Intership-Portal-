using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Job_InternshipPortal.Models
{
    public class JobApplication
    {
        [Key]
        public int Id { get; set; }

        public int JobId { get; set; }

        [ForeignKey("JobId")]
        public Job? Job { get; set; }

        [Required]
        public string ApplicantId { get; set; } = string.Empty;

        [ForeignKey("ApplicantId")]
        public ApplicationUser? Applicant { get; set; }

        [Required]
        [StringLength(1000)]
        public string ResumePath { get; set; } = string.Empty;

        [Required]
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

<<<<<<< HEAD
        public DateTime? SelectedDate { get; set; }

=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
        public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    }
}
