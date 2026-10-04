using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    }
}
