using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Job_InternshipPortal.Models.ViewModels;
using Job_InternshipPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
  
    // Module: Company CRUD (Secured with Ownership Authorization)
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationWorkflowService _workflowService;

        public CompaniesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IApplicationWorkflowService workflowService)
        {
            _context = context;
            _userManager = userManager;
            _workflowService = workflowService;
        }

        // 0. GET: /Companies/Dashboard  (Recruiter only)
        [Authorize(Roles = UserRoles.Recruiter)]
        public async Task<IActionResult> Dashboard()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
                return Challenge();

            var user = await _userManager.FindByIdAsync(currentUserId);
            var now = DateTime.UtcNow;

            // ── Company owned by this recruiter ────────────────────────────────
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.RecruiterId == currentUserId);

            // If recruiter has no company yet, send them to create one
            if (company == null)
            {
                TempData["Error"] = "Please create your company profile to access your dashboard.";
                return RedirectToAction(nameof(Create));
            }

            // ── All job IDs belonging to this company ─────────────────────────
            var jobIds = await _context.Jobs
                .Where(j => j.CompanyId == company.Id)
                .Select(j => new { j.Id, j.Status, j.LastDate })
                .ToListAsync();

            var myJobIdList = jobIds.Select(j => j.Id).ToList();

            // ── Application counts for this company's jobs ────────────────────
            var appStatuses = await _context.JobApplications
                .Where(ja => myJobIdList.Contains(ja.JobId))
                .Select(ja => ja.Status)
                .ToListAsync();

            // ── Upcoming interviews for this company's jobs ───────────────────
            var upcomingInterviews = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Applicant)
                .Where(i => i.JobApplication != null &&
                            myJobIdList.Contains(i.JobApplication.JobId) &&
                            i.JobApplication.Status == ApplicationStatus.Interview &&
                            i.Status == InterviewStatus.Scheduled &&
                            i.InterviewDate > now)
                .OrderBy(i => i.InterviewDate)
                .ToListAsync();

            // ── Recent job listings (latest 5 full nav) ───────────────────────
            var recentJobs = await _context.Jobs
                .Include(j => j.JobApplications)
                .Where(j => j.CompanyId == company.Id)
                .OrderByDescending(j => j.CreatedDate)
                .Take(5)
                .ToListAsync();

            // ── Recent applicants (latest 5 across all company jobs) ──────────
            var recentApplicants = await _context.JobApplications
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Job)
                .Include(ja => ja.Interviews)
                .Where(ja => myJobIdList.Contains(ja.JobId))
                .OrderByDescending(ja => ja.AppliedDate)
                .Take(5)
                .ToListAsync();

            var vm = new RecruiterDashboardViewModel
            {
                RecruiterName = user?.FullName ?? User.Identity?.Name ?? "Recruiter",
                CompanyName   = company.CompanyName,
                CompanyId     = company.Id,

                TotalJobsPosted          = jobIds.Count,
                ActiveJobs               = jobIds.Count(j => j.Status == JobStatus.Approved),
                PendingJobs              = jobIds.Count(j => j.Status == JobStatus.Pending),
                ClosedOrExpiredJobs      = jobIds.Count(j => j.Status == JobStatus.Closed || j.Status == JobStatus.Rejected),

                TotalApplicationsReceived = appStatuses.Count,
                ApplicationsAwaitingReview = appStatuses.Count(s => s == ApplicationStatus.Applied),
                ApplicationsUnderReview  = appStatuses.Count(s => s == ApplicationStatus.UnderReview),
                ShortlistedApplicants     = appStatuses.Count(s => s == ApplicationStatus.Shortlisted),
                SelectedCandidates        = appStatuses.Count(s => s == ApplicationStatus.Selected),
                RejectedApplications     = appStatuses.Count(s => s == ApplicationStatus.Rejected),

                UpcomingInterviewCount = upcomingInterviews.Count,

                RecentJobs        = recentJobs,
                RecentApplicants  = recentApplicants,
                UpcomingInterviews = upcomingInterviews,
            };

            return View(vm);
        }

        // 1. GET: Companies
        public async Task<IActionResult> Index(string? searchString)
        {
            var query = _context.Companies
                .Include(c => c.Recruiter)
                .Include(c => c.Jobs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(c => c.CompanyName.Contains(searchString) ||
                                         (c.Location != null && c.Location.Contains(searchString)) ||
                                         (c.Description != null && c.Description.Contains(searchString)));
                ViewData["CurrentSearch"] = searchString;
            }

            var companies = await query.OrderBy(c => c.CompanyName).ToListAsync();
            return View(companies);
        }

        // 2. GET: Companies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies
                .Include(c => c.Recruiter)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // 3. GET: Companies/Create
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Create()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            // Recruiter can only have one company profile
            if (!User.IsInRole(UserRoles.Admin))
            {
                var existingCompany = await _context.Companies.FirstOrDefaultAsync(c => c.RecruiterId == currentUserId);
                if (existingCompany != null)
                {
                    TempData["Error"] = "You already have a company profile.";
                    return RedirectToAction(nameof(Edit), new { id = existingCompany.Id });
                }
            }
            else
            {
                var recruitersWithCompany = await _context.Companies.Select(c => c.RecruiterId).ToListAsync();
                var recruiterUsers = (await _userManager.GetUsersInRoleAsync(UserRoles.Recruiter))
                    .Where(u => !recruitersWithCompany.Contains(u.Id))
                    .Select(u => new { Id = u.Id, Name = $"{u.FullName} ({u.Email})" })
                    .ToList();

                ViewBag.Recruiters = new SelectList(recruiterUsers, "Id", "Name");
            }

            return View();
        }

        // 4. POST: Companies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Create(Company company)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid();
            }

            if (!User.IsInRole(UserRoles.Admin))
            {
                var existingCompany = await _context.Companies.FirstOrDefaultAsync(c => c.RecruiterId == currentUserId);
                if (existingCompany != null)
                {
                    TempData["Error"] = "You already have a company profile.";
                    return RedirectToAction(nameof(Index));
                }

                // Security: Owner MUST automatically be the currently logged-in recruiter.
                // Never accept owner ID from the form.
                company.RecruiterId = currentUserId;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(company.RecruiterId))
                {
                    var adminHasCompany = await _context.Companies.AnyAsync(c => c.RecruiterId == currentUserId);
                    if (!adminHasCompany)
                    {
                        company.RecruiterId = currentUserId;
                    }
                    else
                    {
                        ModelState.AddModelError("RecruiterId", "Please assign a recruiter or company user for this company.");
                    }
                }
                else
                {
                    var existingForRecruiter = await _context.Companies.AnyAsync(c => c.RecruiterId == company.RecruiterId);
                    if (existingForRecruiter)
                    {
                        ModelState.AddModelError("RecruiterId", "The selected user already has an assigned company profile.");
                    }
                }
            }

            ModelState.Remove("Recruiter");
            ModelState.Remove("Jobs");
            ModelState.Remove("RecruiterId");

            if (ModelState.IsValid)
            {
                _context.Add(company);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Company profile '{company.CompanyName}' created successfully.";

                if (User.IsInRole(UserRoles.Admin))
                {
                    return RedirectToAction("Companies", "Admin");
                }

                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole(UserRoles.Admin))
            {
                var recruitersWithCompany = await _context.Companies.Select(c => c.RecruiterId).ToListAsync();
                var recruiterUsers = (await _userManager.GetUsersInRoleAsync(UserRoles.Recruiter))
                    .Where(u => !recruitersWithCompany.Contains(u.Id))
                    .Select(u => new { Id = u.Id, Name = $"{u.FullName} ({u.Email})" })
                    .ToList();

                ViewBag.Recruiters = new SelectList(recruiterUsers, "Id", "Name", company.RecruiterId);
            }

            return View(company);
        }

        // 5. GET: Companies/Edit/5
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            // Ownership check: logged-in user must be the company owner (or Admin)
            if (company.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            return View(company);
        }

        // 6. POST: Companies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Edit(int id, Company company)
        {
            if (id != company.Id)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid();
            }

            var existingCompany = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (existingCompany == null)
            {
                return NotFound();
            }

            // Ownership check: verify existing company in database belongs to current user
            if (existingCompany.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            ModelState.Remove("Recruiter");
            ModelState.Remove("Jobs");
            ModelState.Remove("RecruiterId");

            if (ModelState.IsValid)
            {
                try
                {
                    // Update only permitted company details.
                    // Owner ID / RecruiterId is preserved and cannot be changed through form submission.
                    existingCompany.CompanyName = company.CompanyName;
                    existingCompany.Description = company.Description;
                    existingCompany.Location = company.Location;
                    existingCompany.Website = company.Website;

                    _context.Update(existingCompany);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Company profile updated successfully.";
                    if (User.IsInRole(UserRoles.Admin))
                    {
                        return RedirectToAction("Companies", "Admin");
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompanyExists(existingCompany.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(company);
        }

        // 7. GET: Companies/Delete/5
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            var company = await _context.Companies
                .Include(c => c.Recruiter)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            // Ownership check: logged-in user must be the company owner (or Admin)
            if (company.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            return View(company);
        }

        // 8. POST: Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid();
            }

            var company = await _context.Companies
                .Include(c => c.Jobs)
                    .ThenInclude(j => j.JobApplications)
                        .ThenInclude(ja => ja.Interviews)
                .Include(c => c.Jobs)
                    .ThenInclude(j => j.SavedJobs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            // Ownership check: logged-in user must be the company owner (or Admin)
            if (company.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            foreach (var job in company.Jobs)
            {
                if (job.SavedJobs != null && job.SavedJobs.Any())
                {
                    _context.SavedJobs.RemoveRange(job.SavedJobs);
                }
                foreach (var app in job.JobApplications)
                {
                    if (app.Interviews != null && app.Interviews.Any())
                    {
                        _context.Interviews.RemoveRange(app.Interviews);
                    }
                }
                _context.JobApplications.RemoveRange(job.JobApplications);
            }
            _context.Jobs.RemoveRange(company.Jobs);
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Company '{company.CompanyName}' deleted successfully.";

            if (User.IsInRole(UserRoles.Admin))
            {
                return RedirectToAction("Companies", "Admin");
            }

            return RedirectToAction(nameof(Index));
        }

        // 9. GET: Companies/ShortlistedApplicants
        [HttpGet]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> ShortlistedApplicants(int? jobId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            Company? company = null;
            if (!isAdmin)
            {
                company = await _context.Companies.FirstOrDefaultAsync(c => c.RecruiterId == currentUserId);
                if (company == null)
                {
                    TempData["Error"] = "Please create your company profile before viewing shortlisted applicants.";
                    return RedirectToAction(nameof(Create));
                }
            }

            var jobsQuery = _context.Jobs.AsQueryable();
            if (!isAdmin)
            {
                jobsQuery = jobsQuery.Where(j => j.CompanyId == company!.Id);
            }
            var companyJobs = await jobsQuery.OrderBy(j => j.Title).ToListAsync();
            ViewBag.Jobs = new SelectList(companyJobs, "Id", "Title", jobId);
            ViewBag.SelectedJobId = jobId;

            var query = _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .Where(ja => ja.Status == ApplicationStatus.Shortlisted);

            if (!isAdmin)
            {
                query = query.Where(ja => ja.Job != null && ja.Job.CompanyId == company!.Id);
            }

            if (jobId.HasValue && jobId.Value > 0)
            {
                query = query.Where(ja => ja.JobId == jobId.Value);
            }

            var shortlisted = await query.OrderByDescending(ja => ja.AppliedDate).ToListAsync();

            // Single placement tracking: identify any candidates already selected for another job
            var applicantIds = shortlisted.Select(a => a.ApplicantId).Distinct().ToList();
            var placedCandidates = await _context.JobApplications
                .Where(ja => applicantIds.Contains(ja.ApplicantId) && ja.Status == ApplicationStatus.Selected)
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Select(ja => new {
                    ja.ApplicantId,
                    JobTitle = ja.Job != null ? ja.Job.Title : "Job",
                    CompanyName = (ja.Job != null && ja.Job.Company != null) ? ja.Job.Company.CompanyName : "Company"
                })
                .ToListAsync();

            ViewBag.PlacedCandidates = placedCandidates
                .GroupBy(p => p.ApplicantId)
                .ToDictionary(g => g.Key, g => $"{g.First().JobTitle} at {g.First().CompanyName}");

            return View(shortlisted);
        }

        [HttpGet("Companies/Shortlisted")]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public Task<IActionResult> Shortlisted(int? jobId) => ShortlistedApplicants(jobId);

        // 10. GET: Companies/SelectedCandidates
        [HttpGet]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> SelectedCandidates(int? jobId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            Company? company = null;
            if (!isAdmin)
            {
                company = await _context.Companies.FirstOrDefaultAsync(c => c.RecruiterId == currentUserId);
                if (company == null)
                {
                    TempData["Error"] = "Please create your company profile before viewing selected candidates.";
                    return RedirectToAction(nameof(Create));
                }
            }

            var jobsQuery = _context.Jobs.AsQueryable();
            if (!isAdmin)
            {
                jobsQuery = jobsQuery.Where(j => j.CompanyId == company!.Id);
            }
            var companyJobs = await jobsQuery.OrderBy(j => j.Title).ToListAsync();
            ViewBag.Jobs = new SelectList(companyJobs, "Id", "Title", jobId);
            ViewBag.SelectedJobId = jobId;

            var query = _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .Where(ja => ja.Status == ApplicationStatus.Selected);

            if (!isAdmin)
            {
                query = query.Where(ja => ja.Job != null && ja.Job.CompanyId == company!.Id);
            }

            if (jobId.HasValue && jobId.Value > 0)
            {
                query = query.Where(ja => ja.JobId == jobId.Value);
            }

            var selected = await query
                .OrderByDescending(ja => ja.SelectedDate ?? ja.AppliedDate)
                .ToListAsync();

            return View(selected);
        }

        [HttpGet("Companies/Selected")]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public Task<IActionResult> Selected(int? jobId) => SelectedCandidates(jobId);

        // 11. POST: Companies/MarkUnderReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> MarkUnderReview(int id, string? returnUrl = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            var application = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            if (!isAdmin && application.Job?.Company?.RecruiterId != currentUserId)
            {
                return Forbid();
            }

            var validation = _workflowService.ValidateTransition(application.Status, ApplicationStatus.UnderReview);
            if (!validation.IsAllowed)
            {
                TempData["Error"] = validation.ErrorMessage ?? "Invalid status change: Application cannot be marked Under Review from its current status.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Dashboard));
            }

            application.Status = ApplicationStatus.UnderReview;
            _context.Update(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Application for '{application.Applicant?.FullName ?? "Applicant"}' is now Under Review.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Details", "JobApplications", new { id });
        }

        // 12. POST: Companies/SelectCandidate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> SelectCandidate(int id, string? returnUrl = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            var application = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            // Server-side ownership verification: verify logged-in recruiter owns the job's company
            if (!isAdmin && application.Job?.Company?.RecruiterId != currentUserId)
            {
                return Forbid();
            }

            var validation = _workflowService.ValidateTransition(application.Status, ApplicationStatus.Selected);
            if (!validation.IsAllowed)
            {
                TempData["Error"] = validation.ErrorMessage ?? $"Invalid status change: Candidate can only be selected from Shortlisted or Interview stages. Current status is '{application.Status}'.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(ShortlistedApplicants));
            }

            // Single Placement Policy: Check if this candidate is already selected for another job across the portal
            var alreadySelected = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(ja => ja.ApplicantId == application.ApplicantId
                                        && ja.Id != application.Id
                                        && ja.Status == ApplicationStatus.Selected);

            if (alreadySelected != null)
            {
                var otherJob = alreadySelected.Job?.Title ?? "another position";
                var otherCompany = alreadySelected.Job?.Company?.CompanyName ?? "another company";
                TempData["Error"] = $"Selection Denied: Candidate '{application.Applicant?.FullName ?? "Candidate"}' has already been selected for '{otherJob}' at '{otherCompany}'. Under the portal's single placement policy, a student cannot be selected for multiple jobs.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(ShortlistedApplicants));
            }

            // Update status and capture selection timestamp
            application.Status = ApplicationStatus.Selected;
            application.SelectedDate = DateTime.UtcNow;

            if (application.Interviews != null)
            {
                foreach (var iv in application.Interviews.Where(i => i.Status == InterviewStatus.Scheduled))
                {
                    iv.Status = InterviewStatus.Completed;
                    _context.Update(iv);
                }
            }

            _context.Update(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Candidate '{application.Applicant?.FullName ?? "Applicant"}' has been successfully selected for '{application.Job?.Title}'!";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(SelectedCandidates));
        }

        // 13. POST: Companies/RejectCandidate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> RejectCandidate(int id, string? returnUrl = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            var application = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            // Server-side ownership verification
            if (!isAdmin && application.Job?.Company?.RecruiterId != currentUserId)
            {
                return Forbid();
            }

            var validation = _workflowService.ValidateTransition(application.Status, ApplicationStatus.Rejected);
            if (!validation.IsAllowed)
            {
                TempData["Error"] = validation.ErrorMessage ?? "Application cannot be rejected from its current status.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(ShortlistedApplicants));
            }

            application.Status = ApplicationStatus.Rejected;

            if (application.Interviews != null)
            {
                foreach (var iv in application.Interviews.Where(i => i.Status == InterviewStatus.Scheduled))
                {
                    iv.Status = InterviewStatus.Cancelled;
                    _context.Update(iv);
                }
            }

            _context.Update(application);
            await _context.SaveChangesAsync();

            TempData["Info"] = $"Candidate '{application.Applicant?.FullName ?? "Applicant"}' was marked as rejected.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Details", "JobApplications", new { id });
        }

        // 14. POST: Companies/ShortlistCandidate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> ShortlistCandidate(int id, string? returnUrl = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(UserRoles.Admin);

            var application = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            // Server-side ownership verification
            if (!isAdmin && application.Job?.Company?.RecruiterId != currentUserId)
            {
                return Forbid();
            }

            var validation = _workflowService.ValidateTransition(application.Status, ApplicationStatus.Shortlisted);
            if (!validation.IsAllowed)
            {
                TempData["Error"] = validation.ErrorMessage ?? "Candidate cannot be shortlisted from current status.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(ShortlistedApplicants), new { jobId = application.JobId });
            }

            application.Status = ApplicationStatus.Shortlisted;
            _context.Update(application);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Candidate '{application.Applicant?.FullName ?? "Applicant"}' has been moved to Shortlisted!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(ShortlistedApplicants), new { jobId = application.JobId });
        }

        private bool CompanyExists(int id)
        {
            return _context.Companies.Any(e => e.Id == id);
        }
    }
}

