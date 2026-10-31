using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
<<<<<<< HEAD
=======

>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
namespace Job_InternshipPortal.Models
{
    public class Interview
    {
        [Key]
        public int Id { get; set; }

        public int JobApplicationId { get; set; }

        [ForeignKey("JobApplicationId")]
        public JobApplication? JobApplication { get; set; }

        [Required]
        public DateTime InterviewDate { get; set; }

        [Required]
        public InterviewType InterviewType { get; set; }

        [StringLength(500)]
        public string? MeetingLink { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }
<<<<<<< HEAD

        // Track interview lifecycle (scheduled, completed, cancelled)
        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
    }
}
