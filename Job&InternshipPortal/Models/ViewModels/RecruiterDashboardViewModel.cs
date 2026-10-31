using Job_InternshipPortal.Models;

namespace Job_InternshipPortal.Models.ViewModels
{
    public class RecruiterDashboardViewModel
    {
        // ── User / company identity ────────────────────────────────────────────
        public string RecruiterName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int? CompanyId { get; set; }

        // ── Job stats ─────────────────────────────────────────────────────────
        public int TotalJobsPosted { get; set; }
        public int ActiveJobs { get; set; }       // Approved
        public int PendingJobs { get; set; }      // Pending
        public int ClosedOrExpiredJobs { get; set; } // Closed | Rejected

        // ── Application stats ─────────────────────────────────────────────────
        public int TotalApplicationsReceived { get; set; }
        public int ApplicationsAwaitingReview { get; set; }  // Applied
        public int ApplicationsUnderReview { get; set; }     // UnderReview
        public int ShortlistedApplicants { get; set; }
        public int SelectedCandidates { get; set; }
        public int RejectedApplications { get; set; }

        // ── Interview stats ───────────────────────────────────────────────────
        public int UpcomingInterviewCount { get; set; }

        // ── Recent job listings (latest 5) ────────────────────────────────────
        public IList<Job> RecentJobs { get; set; } = new List<Job>();

        // ── Recent applicants (latest 5 across all company jobs) ──────────────
        public IList<JobApplication> RecentApplicants { get; set; } = new List<JobApplication>();

        // ── Upcoming interviews (for company jobs, Scheduled & future) ────────
        public IList<Interview> UpcomingInterviews { get; set; } = new List<Interview>();
    }
}
