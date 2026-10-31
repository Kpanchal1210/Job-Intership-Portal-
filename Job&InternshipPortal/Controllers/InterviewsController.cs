using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Job_InternshipPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{

    [Authorize]
    public class InterviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationWorkflowService _workflowService;

        public InterviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IApplicationWorkflowService workflowService)
        {
            _context = context;
            _userManager = userManager;
            _workflowService = workflowService;
        }

        // 1. GET: Interviews
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var isRecruiter = User.IsInRole(UserRoles.Recruiter);
            var isJobSeeker = User.IsInRole(UserRoles.JobSeeker);

            var query = _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                        .ThenInclude(j => j!.Company)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Applicant)
                .AsQueryable();

            if (isRecruiter && !User.IsInRole(UserRoles.Admin))
            {
                query = query.Where(i => i.JobApplication != null &&
                                         i.JobApplication.Job != null &&
                                         i.JobApplication.Job.Company != null &&
                                         i.JobApplication.Job.Company.RecruiterId == currentUserId);
            }
            else if (isJobSeeker && !User.IsInRole(UserRoles.Admin))
            {
                query = query.Where(i => i.JobApplication != null && i.JobApplication.ApplicantId == currentUserId);
            }

            var interviews = await query.ToListAsync();
            return View(interviews);
        }

        // 2. GET: Interviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                        .ThenInclude(j => j!.Company)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (interview == null)
            {
                return NotFound();
            }

            var isRecruiterOwner = interview.JobApplication?.Job?.Company?.RecruiterId == currentUserId;
            var isApplicant = interview.JobApplication?.ApplicantId == currentUserId;
            if (!isRecruiterOwner && !isApplicant && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            return View(interview);
        }

        // 3. GET: Interviews/Create
        [Authorize(Roles = UserRoles.Recruiter)]
        public async Task<IActionResult> Create(int? applicationId = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid();
            }

            var interview = new Interview
            {
                InterviewDate = DateTime.Now.AddDays(3).Date.AddHours(14),
                InterviewType = InterviewType.Online,
                MeetingLink = "https://meet.google.com/portal-interview"
            };

            if (applicationId.HasValue)
            {
                var targetApplication = await _context.JobApplications
                    .Include(ja => ja.Job)
                        .ThenInclude(j => j!.Company)
                    .Include(ja => ja.Applicant)
                    .Include(ja => ja.Interviews)
                    .FirstOrDefaultAsync(ja => ja.Id == applicationId.Value);

                if (targetApplication == null)
                {
                    return NotFound();
                }

                // Security: Recruiter ownership check
                if (targetApplication.Job?.Company?.RecruiterId != currentUserId)
                {
                    return Forbid();
                }

                // Single Placement Policy: prevent scheduling interview if candidate already placed in another job
                if (!string.IsNullOrEmpty(targetApplication.ApplicantId))
                {
                    var placedJob = await _context.JobApplications
                        .Include(ja => ja.Job)
                            .ThenInclude(j => j!.Company)
                        .Where(ja => ja.ApplicantId == targetApplication.ApplicantId && ja.Status == ApplicationStatus.Selected && ja.Id != targetApplication.Id)
                        .FirstOrDefaultAsync();

                    if (placedJob != null)
                    {
                        var candName = targetApplication.Applicant?.FullName ?? targetApplication.Applicant?.UserName ?? "Candidate";
                        var placedJobTitle = placedJob.Job?.Title ?? "another position";
                        var compName = placedJob.Job?.Company?.CompanyName ?? "a company";
                        TempData["Error"] = $"Candidate '{candName}' has already been selected for '{placedJobTitle}' at '{compName}'. Under the portal's single placement policy, this candidate cannot be interviewed.";
                        return RedirectToAction("Details", "JobApplications", new { id = targetApplication.Id });
                    }
                }

                // Prevent scheduling interviews for Selected, Rejected, or non-Shortlisted applications
                if (targetApplication.Status == ApplicationStatus.Selected)
                {
                    TempData["Error"] = "Candidate has already been selected for this position. Cannot schedule an interview.";
                    return RedirectToAction("Details", "JobApplications", new { id = targetApplication.Id });
                }

                if (targetApplication.Status == ApplicationStatus.Rejected)
                {
                    TempData["Error"] = "This application has been rejected. Cannot schedule an interview.";
                    return RedirectToAction("Details", "JobApplications", new { id = targetApplication.Id });
                }

                if (targetApplication.Status != ApplicationStatus.Shortlisted && targetApplication.Status != ApplicationStatus.UnderReview)
                {
                    TempData["Error"] = "Candidate must be Shortlisted or Under Review before scheduling an interview.";
                    return RedirectToAction("Details", "JobApplications", new { id = targetApplication.Id });
                }

                // Prevent duplicate interviews
                if (targetApplication.Interviews != null && targetApplication.Interviews.Any())
                {
                    var existing = targetApplication.Interviews.First();
                    TempData["Info"] = "Interview already scheduled for this application.";
                    return RedirectToAction(nameof(Details), new { id = existing.Id });
                }

                interview.JobApplicationId = applicationId.Value;
                interview.JobApplication = targetApplication;
                ViewBag.CandidateName = targetApplication.Applicant?.FullName ?? targetApplication.Applicant?.UserName ?? "Candidate";
                ViewBag.JobTitle = targetApplication.Job?.Title ?? "Job";
            }
            else
            {
                PopulateApplicationsDropDownList(null);
            }

            return View(interview);
        }

        // 4. POST: Interviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Recruiter)]
        public async Task<IActionResult> Create(Interview interview, string? notes)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid();
            }

            var targetApplication = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .FirstOrDefaultAsync(ja => ja.Id == interview.JobApplicationId);

            if (targetApplication == null)
            {
                ModelState.AddModelError("JobApplicationId", "Selected job application does not exist.");
            }
            else if (targetApplication.Job?.Company?.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }
            else if (await _context.Interviews.AnyAsync(i => i.JobApplicationId == interview.JobApplicationId))
            {
                var existing = await _context.Interviews.FirstAsync(i => i.JobApplicationId == interview.JobApplicationId);
                TempData["Info"] = "Interview already scheduled for this application.";
                return RedirectToAction(nameof(Details), new { id = existing.Id });
            }
            else if (!string.IsNullOrEmpty(targetApplication.ApplicantId) &&
                     await _context.JobApplications.AnyAsync(ja => ja.ApplicantId == targetApplication.ApplicantId && ja.Status == ApplicationStatus.Selected && ja.Id != targetApplication.Id))
            {
                var candName = targetApplication.Applicant?.FullName ?? targetApplication.Applicant?.UserName ?? "Candidate";
                ModelState.AddModelError("JobApplicationId", $"Candidate '{candName}' has already been selected for another position. Candidates placed elsewhere cannot be scheduled for interviews.");
            }
            else
            {
                var validation = _workflowService.ValidateTransition(targetApplication.Status, ApplicationStatus.Interview);
                if (!validation.IsAllowed)
                {
                    ModelState.AddModelError("JobApplicationId", validation.ErrorMessage ?? "Candidate is not in an eligible stage (Shortlisted) to schedule an interview.");
                }
            }

            ModelState.Remove("JobApplication");

            if (ModelState.IsValid && targetApplication != null)
            {
                // Store optional notes in Location / MeetingLink without adding duplicate/unnecessary properties
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    if (interview.InterviewType == InterviewType.Online && string.IsNullOrEmpty(interview.Location))
                    {
                        interview.Location = notes.Trim();
                    }
                    else if (!string.IsNullOrEmpty(interview.Location))
                    {
                        interview.Location = $"{interview.Location.Trim()} (Notes: {notes.Trim()})";
                    }
                    else
                    {
                        interview.Location = notes.Trim();
                    }
                }

                // Transactional consistency: interview creation and status update committed atomically
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Add(interview);

                    // Update Application Status to Interview
                    targetApplication.Status = ApplicationStatus.Interview;
                    _context.Update(targetApplication);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Interview scheduled successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            if (targetApplication != null)
            {
                interview.JobApplication = targetApplication;
                ViewBag.CandidateName = targetApplication.Applicant?.FullName ?? targetApplication.Applicant?.UserName ?? "Candidate";
                ViewBag.JobTitle = targetApplication.Job?.Title ?? "Job";
            }
            else
            {
                PopulateApplicationsDropDownList(interview.JobApplicationId);
            }

            return View(interview);
        }

        // 5. GET: Interviews/Edit/5
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                        .ThenInclude(j => j!.Company)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Applicant)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview == null)
            {
                return NotFound();
            }

            if (interview.JobApplication?.Job?.Company?.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            // Ensure the JobApplication navigation and Applicant are loaded so the view can show the candidate name
            if (interview.JobApplication == null)
            {
                interview.JobApplication = await _context.JobApplications
                    .Include(ja => ja.Applicant)
                    .Include(ja => ja.Job)
                    .FirstOrDefaultAsync(ja => ja.Id == interview.JobApplicationId);
            }

            // Ensure the view has candidate and job display values (avoid showing the literal "Candidate")
            ViewBag.CandidateName = interview.JobApplication?.Applicant?.FullName ?? interview.JobApplication?.Applicant?.UserName ?? "Candidate";
            ViewBag.JobTitle = interview.JobApplication?.Job?.Title ?? "Job";

            // Do not allow changing the associated application from the Edit view; show read-only info instead
            return View(interview);
        }

        // 6. POST: Interviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Edit(int id, Interview interview)
        {
            if (id != interview.Id)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var existingInterview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                        .ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (existingInterview == null)
            {
                return NotFound();
            }

            if (existingInterview.JobApplication?.Job?.Company?.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            ModelState.Remove("JobApplication");

            if (ModelState.IsValid)
            {
                try
                {
                    existingInterview.InterviewDate = interview.InterviewDate;
                    existingInterview.InterviewType = interview.InterviewType;
                    existingInterview.MeetingLink = interview.MeetingLink;
                    existingInterview.Location = interview.Location;
                    existingInterview.Status = interview.Status;

                    _context.Update(existingInterview);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InterviewExists(existingInterview.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateApplicationsDropDownList(interview.JobApplicationId);
            return View(interview);
        }

        // 7. GET: Interviews/Delete/5
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                        .ThenInclude(j => j!.Company)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (interview == null)
            {
                return NotFound();
            }

            if (interview.JobApplication?.Job?.Company?.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            return View(interview);
        }

        // 8. POST: Interviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                        .ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview == null)
            {
                return NotFound();
            }

            if (interview.JobApplication?.Job?.Company?.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            _context.Interviews.Remove(interview);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private void PopulateApplicationsDropDownList(object? selectedApplication = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            var query = _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .AsQueryable();

            if (!User.IsInRole(UserRoles.Admin))
            {
                query = query.Where(ja => ja.Job != null && ja.Job.Company != null && ja.Job.Company.RecruiterId == currentUserId);
            }

            // Shortlisted and Under Review applications without an existing interview are eligible
            query = query.Where(ja => (ja.Status == ApplicationStatus.Shortlisted || ja.Status == ApplicationStatus.UnderReview) && !ja.Interviews.Any());

            // Single Placement Policy: Exclude candidates who have already been selected for another job
            var placedApplicantIds = _context.JobApplications
                .Where(ja => ja.Status == ApplicationStatus.Selected)
                .Select(ja => ja.ApplicantId)
                .Distinct();

            query = query.Where(ja => !placedApplicantIds.Contains(ja.ApplicantId));

            var applications = query
                .Select(ja => new
                {
                    ja.Id,
                    Display = (ja.Applicant != null ? (ja.Applicant.FullName ?? ja.Applicant.UserName) : "Candidate")
                              + " - " + (ja.Job != null ? ja.Job.Title : "Job")
                })
                .ToList();

            ViewData["JobApplicationId"] = new SelectList(applications, "Id", "Display", selectedApplication);
        }

        private bool InterviewExists(int id)
        {
            return _context.Interviews.Any(e => e.Id == id);
        }
    }
}

