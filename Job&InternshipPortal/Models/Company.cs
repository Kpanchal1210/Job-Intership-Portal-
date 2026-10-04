using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobInternshipPortal.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RecruiterId { get; set; } = string.Empty;

        [ForeignKey("RecruiterId")]
        public ApplicationUser Recruiter { get; set; };

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Location { get; set; }

        public ICollection<Job> Jobs { get; set; } = new List<Job>();
    }
}
