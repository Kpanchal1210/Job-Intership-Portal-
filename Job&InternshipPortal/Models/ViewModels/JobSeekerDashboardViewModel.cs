using Job_InternshipPortal.Models;

namespace Job_InternshipPortal.Models.ViewModels
{
    public class JobSeekerDashboardViewModel
    {
        // ── User identity ──────────────────────────────────────────────────────
        public string FullName { get; set; } = string.Empty;

        // ── Stats ──────────────────────────────────────────────────────────────
        public int TotalApplications { get; set; }
        public int AppliedCount { get; set; }
        public int ApplicationsUnderReview { get; set; }
        public int Shortlisted { get; set; }
        public int Selected { get; set; }
        public int RejectedApplications { get; set; }
        public int UpcomingInterviewCount { get; set; }
        public int SavedJobsCount { get; set; }

        // ── Recent applications (latest 5) ────────────────────────────────────
        public IList<JobApplication> RecentApplications { get; set; } = new List<JobApplication>();

        // ── Filtered status application lists ─────────────────────────────────
        public IList<JobApplication> ShortlistedApplicationsList { get; set; } = new List<JobApplication>();
        public IList<JobApplication> SelectedApplicationsList { get; set; } = new List<JobApplication>();

        // ── Upcoming interviews (future, Scheduled only) ──────────────────────
        public IList<Interview> UpcomingInterviews { get; set; } = new List<Interview>();

        // ── Recently saved jobs (latest 5) ────────────────────────────────────
        public IList<SavedJob> RecentSavedJobs { get; set; } = new List<SavedJob>();

        // ── Recommended jobs (Approved, not yet applied, latest 6) ───────────
        public IList<Job> RecommendedJobs { get; set; } = new List<Job>();
    }
}
