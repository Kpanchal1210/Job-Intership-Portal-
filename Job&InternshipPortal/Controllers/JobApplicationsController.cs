<<<<<<< HEAD

using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Job_InternshipPortal.Models.ViewModels;
using Job_InternshipPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
=======
using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
<<<<<<< HEAD
    [Authorize]
    public class JobApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationWorkflowService _workflowService;
        private readonly IWebHostEnvironment? _webHostEnvironment;

        // ── Allowed resume file extensions (allowlist, never a denylist) ──────
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx" };

        // ── Maximum upload size: 5 MB ─────────────────────────────────────────
        private const long MaxResumeBytes = 5 * 1024 * 1024;

        public JobApplicationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IApplicationWorkflowService workflowService,
            IWebHostEnvironment? webHostEnvironment = null)
        {
            _context = context;
            _userManager = userManager;
            _workflowService = workflowService;
            _webHostEnvironment = webHostEnvironment;
        }

        // ── 0. GET: /JobApplications/Dashboard  (Job Seeker only) ─────────────
        [Authorize(Roles = UserRoles.JobSeeker)]
        public async Task<IActionResult> Dashboard()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
                return Challenge();

            var user = await _userManager.FindByIdAsync(currentUserId);
            var now = DateTime.UtcNow;

            var myApplicationStatuses = await _context.JobApplications
                .Where(ja => ja.ApplicantId == currentUserId)
                .Select(ja => new { ja.Id, ja.Status })
                .ToListAsync();

            var upcomingInterviews = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                        .ThenInclude(j => j!.Company)
                .Where(i => i.JobApplication != null &&
                            i.JobApplication.ApplicantId == currentUserId &&
                            i.JobApplication.Status == ApplicationStatus.Interview &&
                            i.Status == InterviewStatus.Scheduled &&
                            i.InterviewDate > now)
                .OrderBy(i => i.InterviewDate)
                .ToListAsync();

            var savedJobsCount = await _context.SavedJobs
                .CountAsync(s => s.ApplicantId == currentUserId);

            var recentApplications = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Interviews)
                .Where(ja => ja.ApplicantId == currentUserId)
                .OrderByDescending(ja => ja.AppliedDate)
                .Take(5)
                .ToListAsync();

            var recentSavedJobs = await _context.SavedJobs
                .Include(s => s.Job)
                    .ThenInclude(j => j!.Company)
                .Where(s => s.ApplicantId == currentUserId)
                .OrderByDescending(s => s.SavedDate)
                .Take(5)
                .ToListAsync();

            var appliedToJobIds = await _context.JobApplications
                .Where(ja => ja.ApplicantId == currentUserId)
                .Select(ja => ja.JobId)
                .ToListAsync();

            var recommendedJobs = await _context.Jobs
                .Include(j => j.Company)
                .Where(j => j.Status == JobStatus.Approved &&
                            !appliedToJobIds.Contains(j.Id) &&
                            j.LastDate >= now)
                .OrderByDescending(j => j.CreatedDate)
                .Take(6)
                .ToListAsync();

            var shortlistedApps = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Interviews)
                .Where(ja => ja.ApplicantId == currentUserId && ja.Status == ApplicationStatus.Shortlisted)
                .OrderByDescending(ja => ja.AppliedDate)
                .ToListAsync();

            var selectedApps = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Interviews)
                .Where(ja => ja.ApplicantId == currentUserId && ja.Status == ApplicationStatus.Selected)
                .OrderByDescending(ja => ja.SelectedDate ?? ja.AppliedDate)
                .ToListAsync();

            var vm = new JobSeekerDashboardViewModel
            {
                FullName = user?.FullName ?? User.Identity?.Name ?? "Job Seeker",

                TotalApplications       = myApplicationStatuses.Count,
                AppliedCount            = myApplicationStatuses.Count(a => a.Status == ApplicationStatus.Applied),
                ApplicationsUnderReview = myApplicationStatuses.Count(a => a.Status == ApplicationStatus.UnderReview),
                Shortlisted             = myApplicationStatuses.Count(a => a.Status == ApplicationStatus.Shortlisted),
                Selected                = myApplicationStatuses.Count(a => a.Status == ApplicationStatus.Selected),
                RejectedApplications    = myApplicationStatuses.Count(a => a.Status == ApplicationStatus.Rejected),
                UpcomingInterviewCount  = upcomingInterviews.Count,
                SavedJobsCount          = savedJobsCount,

                RecentApplications          = recentApplications,
                ShortlistedApplicationsList = shortlistedApps,
                SelectedApplicationsList    = selectedApps,
                UpcomingInterviews          = upcomingInterviews,
                RecentSavedJobs             = recentSavedJobs,
                RecommendedJobs             = recommendedJobs,
            };

            return View(vm);
        }

        // ── 1. GET: /JobApplications ──────────────────────────────────────────
        public async Task<IActionResult> Index(int? jobId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isRecruiter   = User.IsInRole(UserRoles.Recruiter);
            var isJobSeeker   = User.IsInRole(UserRoles.JobSeeker);

            var query = _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .AsQueryable();

            if (isRecruiter && !User.IsInRole(UserRoles.Admin))
            {
                query = query.Where(ja =>
                    ja.Job != null &&
                    ja.Job.Company != null &&
                    ja.Job.Company.RecruiterId == currentUserId);
            }
            else if (isJobSeeker && !User.IsInRole(UserRoles.Admin))
            {
                query = query.Where(ja => ja.ApplicantId == currentUserId);
            }

            if (jobId.HasValue && jobId.Value > 0)
            {
                query = query.Where(ja => ja.JobId == jobId.Value);
                ViewBag.SelectedJobId = jobId.Value;

                var filteredJob = await _context.Jobs.FindAsync(jobId.Value);
                ViewBag.FilteredJobTitle = filteredJob?.Title;
            }

            var applications = await query.OrderByDescending(ja => ja.AppliedDate).ToListAsync();

            if (isRecruiter || User.IsInRole(UserRoles.Admin))
            {
                var applicantIds = applications.Select(a => a.ApplicantId).Distinct().ToList();
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
            }

            return View(applications);
        }

        // ── 2. GET: /JobApplications/Details/5 ───────────────────────────────
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var jobApplication = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (jobApplication == null) return NotFound();

            // IDOR: only the job's recruiter, the applicant, or Admin may view
            var isJobOwner  = jobApplication.Job?.Company?.RecruiterId == currentUserId;
            var isApplicant = jobApplication.ApplicantId == currentUserId;
            if (!isJobOwner && !isApplicant && !User.IsInRole(UserRoles.Admin))
                return Forbid();

            // Single placement policy check: verify if candidate has already been selected for another job
            var otherSelectedApp = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(ja => ja.ApplicantId == jobApplication.ApplicantId
                                        && ja.Id != jobApplication.Id
                                        && ja.Status == ApplicationStatus.Selected);

            if (otherSelectedApp != null)
            {
                ViewBag.AlreadySelectedForJob = $"{otherSelectedApp.Job?.Title ?? "another position"} at {otherSelectedApp.Job?.Company?.CompanyName ?? "another company"}";
            }

            return View(jobApplication);
        }

        // ── 3. GET: /JobApplications/Create ──────────────────────────────────
        [Authorize(Roles = UserRoles.JobSeeker)]
        public async Task<IActionResult> Create(int? jobId = null)
        {
            var currentUserId = _userManager.GetUserId(User);

            var jobApplication = new JobApplication
            {
                // Pre-populate with safe server-side defaults only
                AppliedDate = DateTime.UtcNow,
                Status      = ApplicationStatus.Applied,
            };

            if (jobId.HasValue)
            {
                jobApplication.JobId = jobId.Value;
                var job = await _context.Jobs
                    .Include(j => j.Company)
                    .FirstOrDefaultAsync(j => j.Id == jobId.Value);
                if (job != null)
                    jobApplication.Job = job;

                // Check if candidate already applied for this job
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var existingApp = await _context.JobApplications
                        .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                        .FirstOrDefaultAsync(ja => ja.JobId == jobId.Value && ja.ApplicantId == currentUserId);
                    if (existingApp != null)
                    {
                        ViewBag.ExistingApplication = existingApp;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentUserId))
                jobApplication.ApplicantId = currentUserId;

            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", jobId);
            return View(jobApplication);
        }

        // ── 4. POST: /JobApplications/Create ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.JobSeeker)]
        public async Task<IActionResult> Create(JobApplication jobApplication, IFormFile? resumeFile)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
                return Forbid();

            // ── SECURITY: verify job exists, is active and deadline has not passed ─
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobApplication.JobId);
            if (job == null)
=======
    // ========================================================
    // 2-Person Project Division: Person 2
    // Module: Job Application CRUD
    // ========================================================
    public class JobApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobApplicationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: JobApplications
        public async Task<IActionResult> Index()
        {
            var jobApplications = await _context.JobApplications
                .Include(ja => ja.Job)
                .Include(ja => ja.Applicant)
                .ToListAsync();

            return View(jobApplications);
        }

        // 2. GET: JobApplications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            {
                return NotFound();
            }

<<<<<<< HEAD
            if (job.Status != JobStatus.Approved || job.LastDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(string.Empty,
                    "This job is no longer active or the application deadline has passed.");
            }

            // ── SECURITY: never trust browser-supplied ownership/status fields ─
            jobApplication.ApplicantId = currentUserId;          // IDOR prevention
            jobApplication.AppliedDate = DateTime.UtcNow;        // over-posting fix
            jobApplication.Status      = ApplicationStatus.Applied; // over-posting fix

            ModelState.Remove("Job");
            ModelState.Remove("Applicant");
            ModelState.Remove("Interviews");
            ModelState.Remove("ResumePath");    // validated separately below

            // ── Resume file validation ────────────────────────────────────────
            if (resumeFile != null && resumeFile.Length > 0)
            {
                // Size check
                if (resumeFile.Length > MaxResumeBytes)
                {
                    ModelState.AddModelError("ResumePath",
                        "Resume file must be 5 MB or smaller.");
                }
                else
                {
                    // Extension allowlist (never rely on ContentType alone — it can be spoofed)
                    var ext = Path.GetExtension(resumeFile.FileName);
                    if (!AllowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ResumePath",
                            "Only PDF, DOC, and DOCX files are accepted.");
                    }
                    else
                    {
                        // Safe filename: strip original name, use only the safe extension
                        var safeFileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
                        var webRoot = _webHostEnvironment?.WebRootPath 
                            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        var uploadsFolder = Path.Combine(webRoot, "uploads", "resumes");
                        Directory.CreateDirectory(uploadsFolder);

                        var filePath = Path.Combine(uploadsFolder, safeFileName);
                        await using var fs = new FileStream(filePath, FileMode.Create);
                        await resumeFile.CopyToAsync(fs);

                        jobApplication.ResumePath = "/uploads/resumes/" + safeFileName;
                    }
                }
            }

            // Fallback path when no file was uploaded
            if (ModelState.IsValid && string.IsNullOrEmpty(jobApplication.ResumePath))
            {
                jobApplication.ResumePath = "/uploads/resumes/sample_resume.pdf";
            }

            // Duplicate application check
            var existingApp = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(a => a.JobId == jobApplication.JobId && a.ApplicantId == jobApplication.ApplicantId);
            if (existingApp != null)
            {
                ModelState.AddModelError(string.Empty, "You have already applied for this job.");
                ViewBag.ExistingApplication = existingApp;
=======
            var jobApplication = await _context.JobApplications
                .Include(ja => ja.Job)
                .Include(ja => ja.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (jobApplication == null)
            {
                return NotFound();
            }

            return View(jobApplication);
        }

        // 3. GET: JobApplications/Create
        public IActionResult Create()
        {
            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName");
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title");
            return View();
        }

        // 4. POST: JobApplications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobApplication jobApplication)
        {
            ModelState.Remove("Job");
            ModelState.Remove("Applicant");
            ModelState.Remove("Interviews");

            if (await _context.JobApplications.AnyAsync(a => a.JobId == jobApplication.JobId && a.ApplicantId == jobApplication.ApplicantId))
            {
                ModelState.AddModelError(string.Empty, "An application for this job and applicant already exists.");
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            }

            if (ModelState.IsValid)
            {
                _context.Add(jobApplication);
                await _context.SaveChangesAsync();
<<<<<<< HEAD
                TempData["Success"] = "Application submitted successfully!";
                return RedirectToAction(nameof(Index));
            }

            if (jobApplication.Job == null && jobApplication.JobId > 0)
            {
                jobApplication.Job = await _context.Jobs
                    .Include(j => j.Company)
                    .FirstOrDefaultAsync(j => j.Id == jobApplication.JobId);
            }

=======
                return RedirectToAction(nameof(Index));
            }

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", jobApplication.ApplicantId);
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", jobApplication.JobId);
            return View(jobApplication);
        }

<<<<<<< HEAD
        // ── 5. GET: /JobApplications/Edit/5  (Recruiter or Admin only) ────────
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var jobApplication = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (jobApplication == null) return NotFound();

            // IDOR: only the job-owning recruiter or Admin may change status
            if (jobApplication.Job?.Company?.RecruiterId != currentUserId &&
                !User.IsInRole(UserRoles.Admin))
                return Forbid();

            // Single placement policy check: verify if candidate is already selected for another job
            var otherSelectedApp = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(ja => ja.ApplicantId == jobApplication.ApplicantId
                                        && ja.Id != jobApplication.Id
                                        && ja.Status == ApplicationStatus.Selected);

            if (otherSelectedApp != null)
            {
                ViewBag.AlreadySelectedForJob = $"{otherSelectedApp.Job?.Title ?? "another position"} at {otherSelectedApp.Job?.Company?.CompanyName ?? "another company"}";
            }

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", jobApplication.ApplicantId);
            ViewData["JobId"]       = new SelectList(_context.Jobs,  "Id", "Title",    jobApplication.JobId);
            return View(jobApplication);
        }

        // ── 6. POST: /JobApplications/Edit/5 ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Edit(int id, JobApplication jobApplication)
        {
            if (id != jobApplication.Id) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var existingApplication = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Interviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingApplication == null) return NotFound();

            // IDOR: re-check ownership against DB record, never the submitted form
            if (existingApplication.Job?.Company?.RecruiterId != currentUserId &&
                !User.IsInRole(UserRoles.Admin))
                return Forbid();
=======
        // 5. GET: JobApplications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jobApplication = await _context.JobApplications.FindAsync(id);
            if (jobApplication == null)
            {
                return NotFound();
            }

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", jobApplication.ApplicantId);
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", jobApplication.JobId);
            return View(jobApplication);
        }

        // 6. POST: JobApplications/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobApplication jobApplication)
        {
            if (id != jobApplication.Id)
            {
                return NotFound();
            }
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169

            ModelState.Remove("Job");
            ModelState.Remove("Applicant");
            ModelState.Remove("Interviews");

<<<<<<< HEAD
            if (jobApplication.Status != existingApplication.Status)
            {
                var validation = _workflowService.ValidateTransition(existingApplication.Status, jobApplication.Status);
                if (!validation.IsAllowed)
                {
                    ModelState.AddModelError("Status", validation.ErrorMessage ?? "Invalid status transition.");
                }

                // Single Placement Policy: A student can only be selected for one job across the portal
                if (jobApplication.Status == ApplicationStatus.Selected)
                {
                    var alreadySelected = await _context.JobApplications
                        .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                        .FirstOrDefaultAsync(ja => ja.ApplicantId == existingApplication.ApplicantId
                                                && ja.Id != existingApplication.Id
                                                && ja.Status == ApplicationStatus.Selected);

                    if (alreadySelected != null)
                    {
                        var otherJob = alreadySelected.Job?.Title ?? "another position";
                        var otherCompany = alreadySelected.Job?.Company?.CompanyName ?? "another company";
                        ModelState.AddModelError("Status", $"Selection Denied: Candidate '{existingApplication.Applicant?.FullName ?? "Candidate"}' has already been selected for '{otherJob}' at '{otherCompany}'. Under the portal's single placement policy, a student cannot be selected for multiple jobs.");
                    }
                }
            }

=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            if (ModelState.IsValid)
            {
                try
                {
<<<<<<< HEAD
                    // Only allow the Status field to be updated — all other fields
                    // come from the DB record to prevent mass-assignment / over-posting
                    existingApplication.Status = jobApplication.Status;

                    if (existingApplication.Status == ApplicationStatus.Selected)
                    {
                        if (existingApplication.SelectedDate == null)
                        {
                            existingApplication.SelectedDate = DateTime.UtcNow;
                        }

                        if (existingApplication.Interviews != null)
                        {
                            foreach (var iv in existingApplication.Interviews
                                         .Where(i => i.Status == InterviewStatus.Scheduled))
                            {
                                iv.Status = InterviewStatus.Completed;
                                _context.Update(iv);
                            }
                        }
                    }
                    else if (existingApplication.Status == ApplicationStatus.Rejected)
                    {
                        if (existingApplication.Interviews != null)
                        {
                            foreach (var iv in existingApplication.Interviews
                                         .Where(i => i.Status == InterviewStatus.Scheduled))
                            {
                                iv.Status = InterviewStatus.Cancelled;
                                _context.Update(iv);
                            }
                        }
                    }

                    _context.Update(existingApplication);
                    await _context.SaveChangesAsync();

                    if (existingApplication.Status == ApplicationStatus.Interview)
                    {
                        var existingInterview = await _context.Interviews
                            .FirstOrDefaultAsync(i => i.JobApplicationId == existingApplication.Id);
                        if (existingInterview != null)
                        {
                            TempData["Info"] = "Interview already scheduled for this application.";
                            return RedirectToAction("Details", "Interviews", new { id = existingInterview.Id });
                        }
                        return RedirectToAction("Create", "Interviews",
                            new { applicationId = existingApplication.Id });
                    }

                    TempData["Success"] = $"Application status updated to {existingApplication.Status}.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobApplicationExists(existingApplication.Id))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Details), new { id = existingApplication.Id });
            }

            jobApplication.Applicant = existingApplication.Applicant;
            jobApplication.Job = existingApplication.Job;

            var otherPlacement = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(ja => ja.ApplicantId == existingApplication.ApplicantId
                                        && ja.Id != existingApplication.Id
                                        && ja.Status == ApplicationStatus.Selected);
            if (otherPlacement != null)
            {
                ViewBag.AlreadySelectedForJob = $"{otherPlacement.Job?.Title ?? "another position"} at {otherPlacement.Job?.Company?.CompanyName ?? "another company"}";
            }

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", existingApplication.ApplicantId);
            ViewData["JobId"]       = new SelectList(_context.Jobs,  "Id", "Title",    existingApplication.JobId);
            return View(jobApplication);
        }

        // ── 7. GET: /JobApplications/Delete/5 ────────────────────────────────
        // [Authorize] inherited from controller; ownership enforced below
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId  = _userManager.GetUserId(User);
            var jobApplication = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .Include(ja => ja.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (jobApplication == null) return NotFound();

            var isJobOwner  = jobApplication.Job?.Company?.RecruiterId == currentUserId;
            var isApplicant = jobApplication.ApplicantId == currentUserId;
            if (!isJobOwner && !isApplicant && !User.IsInRole(UserRoles.Admin))
                return Forbid();
=======
                    _context.Update(jobApplication);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobApplicationExists(jobApplication.Id))
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

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", jobApplication.ApplicantId);
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", jobApplication.JobId);
            return View(jobApplication);
        }

        // 7. GET: JobApplications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jobApplication = await _context.JobApplications
                .Include(ja => ja.Job)
                .Include(ja => ja.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (jobApplication == null)
            {
                return NotFound();
            }
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169

            return View(jobApplication);
        }

<<<<<<< HEAD
        // ── 8. POST: /JobApplications/Delete/5 ───────────────────────────────
=======
        // 8. POST: JobApplications/Delete/5
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
<<<<<<< HEAD
            var currentUserId  = _userManager.GetUserId(User);
            var jobApplication = await _context.JobApplications
                .Include(ja => ja.Job).ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (jobApplication == null) return NotFound();

            // IDOR: re-verify ownership against DB on every destructive POST
            var isJobOwner  = jobApplication.Job?.Company?.RecruiterId == currentUserId;
            var isApplicant = jobApplication.ApplicantId == currentUserId;
            if (!isJobOwner && !isApplicant && !User.IsInRole(UserRoles.Admin))
                return Forbid();

            _context.JobApplications.Remove(jobApplication);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Application removed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public IActionResult Shortlisted(int? jobId)
            => RedirectToAction("ShortlistedApplicants", "Companies", new { jobId });

        [HttpGet]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public IActionResult Selected(int? jobId)
            => RedirectToAction("SelectedCandidates", "Companies", new { jobId });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public Task<IActionResult> MarkUnderReview(int id, string? returnUrl = null)
        {
            var companiesController = new CompaniesController(_context, _userManager, _workflowService)
            {
                ControllerContext = ControllerContext,
                TempData = TempData
            };
            return companiesController.MarkUnderReview(id, returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public Task<IActionResult> ShortlistCandidate(int id, string? returnUrl = null)
        {
            var companiesController = new CompaniesController(_context, _userManager, _workflowService)
            {
                ControllerContext = ControllerContext,
                TempData = TempData
            };
            return companiesController.ShortlistCandidate(id, returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public Task<IActionResult> SelectCandidate(int id, string? returnUrl = null)
        {
            var companiesController = new CompaniesController(_context, _userManager, _workflowService)
            {
                ControllerContext = ControllerContext,
                TempData = TempData
            };
            return companiesController.SelectCandidate(id, returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public Task<IActionResult> RejectCandidate(int id, string? returnUrl = null)
        {
            var companiesController = new CompaniesController(_context, _userManager, _workflowService)
            {
                ControllerContext = ControllerContext,
                TempData = TempData
            };
            return companiesController.RejectCandidate(id, returnUrl);
        }

        private bool JobApplicationExists(int id)
            => _context.JobApplications.Any(e => e.Id == id);
=======
            var jobApplication = await _context.JobApplications.FindAsync(id);
            if (jobApplication != null)
            {
                _context.JobApplications.Remove(jobApplication);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool JobApplicationExists(int id)
        {
            return _context.JobApplications.Any(e => e.Id == id);
        }
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
    }
}
