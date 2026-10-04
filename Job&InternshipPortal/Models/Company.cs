using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Job_InternshipPortal.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(200)]
        [Url]
        public string? Website { get; set; }

        [Required]
        public string RecruiterId { get; set; } = string.Empty;

        [ForeignKey("RecruiterId")]
        public ApplicationUser? Recruiter { get; set; }

        public ICollection<Job> Jobs { get; set; } = new List<Job>();
    }
}
