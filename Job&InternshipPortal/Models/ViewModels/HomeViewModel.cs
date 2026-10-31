namespace Job_InternshipPortal.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Job> FeaturedJobs { get; set; } = new();
        public List<Company> FeaturedCompanies { get; set; } = new();
        public int TotalJobs { get; set; }
        public int TotalInternships { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalApplications { get; set; }
        public string? SearchQuery { get; set; }
        public JobType? JobType { get; set; }
        public string? Location { get; set; }
    }
}
