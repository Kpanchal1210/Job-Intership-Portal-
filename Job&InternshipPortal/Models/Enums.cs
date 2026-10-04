namespace Job_InternshipPortal.Models
{
    public enum JobType
    {
        FullTime,
        PartTime,
        Internship
    }

    public enum JobStatus
    {
        Pending,
        Approved,
        Rejected,
        Closed
    }

    public enum ApplicationStatus
    {
        Applied,
        UnderReview,
        Shortlisted,
        Interview,
        Selected,
        Rejected
    }

    public enum InterviewType
    {
        Online,
        Offline
    }
}
