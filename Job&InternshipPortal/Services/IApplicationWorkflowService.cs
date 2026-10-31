using Job_InternshipPortal.Models;

namespace Job_InternshipPortal.Services
{
    public interface IApplicationWorkflowService
    {
        /// <summary>
        /// Evaluates whether the requested transition between application statuses is permissible.
        /// </summary>
        bool IsValidTransition(ApplicationStatus fromStatus, ApplicationStatus toStatus);

        /// <summary>
        /// Validates the transition and returns a tuple indicating allowance and user-facing error message.
        /// </summary>
        (bool IsAllowed, string? ErrorMessage) ValidateTransition(ApplicationStatus fromStatus, ApplicationStatus toStatus);

        bool CanMarkUnderReview(ApplicationStatus current);
        bool CanShortlist(ApplicationStatus current);
        bool CanScheduleInterview(ApplicationStatus current);
        bool CanSelect(ApplicationStatus current);
        bool CanReject(ApplicationStatus current);
    }
}

