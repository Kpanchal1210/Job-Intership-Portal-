using Job_InternshipPortal.Models;

namespace Job_InternshipPortal.Services
{
    public class ApplicationWorkflowService : IApplicationWorkflowService
    {
        private static readonly Dictionary<ApplicationStatus, HashSet<ApplicationStatus>> AllowedTransitions = new()
        {
            [ApplicationStatus.Applied] = new HashSet<ApplicationStatus>
            {
                ApplicationStatus.UnderReview,
                ApplicationStatus.Shortlisted,
                ApplicationStatus.Rejected
            },
            [ApplicationStatus.UnderReview] = new HashSet<ApplicationStatus>
            {
                ApplicationStatus.Shortlisted,
                ApplicationStatus.Interview,
                ApplicationStatus.Rejected
            },
            [ApplicationStatus.Shortlisted] = new HashSet<ApplicationStatus>
            {
                ApplicationStatus.Interview,
                ApplicationStatus.Selected,
                ApplicationStatus.Rejected
            },
            [ApplicationStatus.Interview] = new HashSet<ApplicationStatus>
            {
                ApplicationStatus.Selected,
                ApplicationStatus.Rejected
            },
            [ApplicationStatus.Selected] = new HashSet<ApplicationStatus>(),
            [ApplicationStatus.Rejected] = new HashSet<ApplicationStatus>()
        };

        public bool IsValidTransition(ApplicationStatus fromStatus, ApplicationStatus toStatus)
        {
            if (AllowedTransitions.TryGetValue(fromStatus, out var allowedTargets))
            {
                return allowedTargets.Contains(toStatus);
            }
            return false;
        }

        public (bool IsAllowed, string? ErrorMessage) ValidateTransition(ApplicationStatus fromStatus, ApplicationStatus toStatus)
        {
            if (fromStatus == toStatus)
            {
                return (false, $"Application is already in '{GetStatusDisplayName(fromStatus)}' status.");
            }

            if (fromStatus == ApplicationStatus.Selected)
            {
                return (false, "Candidate has already been selected. Selected is a terminal status and cannot be changed.");
            }

            if (fromStatus == ApplicationStatus.Rejected)
            {
                return (false, "Application has already been rejected. Rejected is a terminal status and cannot be changed.");
            }

            if (IsValidTransition(fromStatus, toStatus))
            {
                return (true, null);
            }

            return (false, $"Invalid recruitment transition from '{GetStatusDisplayName(fromStatus)}' to '{GetStatusDisplayName(toStatus)}'.");
        }

        public bool CanMarkUnderReview(ApplicationStatus current) => IsValidTransition(current, ApplicationStatus.UnderReview);
        public bool CanShortlist(ApplicationStatus current) => IsValidTransition(current, ApplicationStatus.Shortlisted);
        public bool CanScheduleInterview(ApplicationStatus current) => IsValidTransition(current, ApplicationStatus.Interview);
        public bool CanSelect(ApplicationStatus current) => IsValidTransition(current, ApplicationStatus.Selected);
        public bool CanReject(ApplicationStatus current) => IsValidTransition(current, ApplicationStatus.Rejected);

        private static string GetStatusDisplayName(ApplicationStatus status) => status switch
        {
            ApplicationStatus.UnderReview => "Under Review",
            ApplicationStatus.Shortlisted => "Shortlisted",
            ApplicationStatus.Interview   => "Interview Scheduled",
            ApplicationStatus.Selected    => "Selected",
            ApplicationStatus.Rejected    => "Rejected",
            _                             => "Applied"
        };
    }
}

