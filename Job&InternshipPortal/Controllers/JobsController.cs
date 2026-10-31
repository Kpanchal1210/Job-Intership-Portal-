using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
   
    // Module: Job CRUD (Secured with Company Ownership Authorization)
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. GET: Jobs
        public async Task<IActionResult> Index(string? searchString, JobType? jobType, string? location)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isRecruiter = User.Identity?.IsAuthenticated == true && User.IsInRole(UserRoles.Recruiter);

            var query = _context.Jobs
                .Include(j => j.Company)
                    .ThenInclude(c => c!.Recruiter)
                .Include(j => j.JobApplications)
                .AsQueryable();

            // Recruiters see only their own company's jobs (all statuses)
            // Admins see all jobs (handled separately via Admin/Jobs)
            // Job seekers and guests see only Approved jobs
            if (isRecruiter && !User.IsInRole(UserRoles.Admin))
            {
                query = query.Where(j => j.Company != null && j.Company.RecruiterId == currentUserId);
            }
            else if (!User.IsInRole(UserRoles.Admin))
            {
                // Public / Job Seeker: only show approved jobs
                query = query.Where(j => j.Status == JobStatus.Approved);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(j => j.Title.Contains(searchString) ||
                                         j.Description.Contains(searchString) ||
                                         (j.Company != null && j.Company.CompanyName.Contains(searchString)));
                ViewData["CurrentSearch"] = searchString;
            }

            if (jobType.HasValue)
            {
                query = query.Where(j => j.JobType == jobType.Value);
                ViewData["CurrentJobType"] = jobType.Value;
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(j => j.Location != null && j.Location.Contains(location));
                ViewData["CurrentLocation"] = location;
            }

            // Track applications and saved jobs for the logged-in candidate
            if (!string.IsNullOrEmpty(currentUserId) && User.IsInRole(UserRoles.JobSeeker))
            {
                var appliedJobMap = await _context.JobApplications
                    .Where(a => a.ApplicantId == currentUserId)
                    .ToDictionaryAsync(a => a.JobId, a => a.Id);
                ViewBag.AppliedJobMap = appliedJobMap;

                var savedJobIds = await _context.SavedJobs
                    .Where(s => s.ApplicantId == currentUserId)
                    .Select(s => s.JobId)
                    .ToListAsync();
                ViewBag.SavedJobIds = savedJobIds;
            }

            var jobs = await query.OrderByDescending(j => j.CreatedDate).ToListAsync();
            return View(jobs);
        }

        // 2. GET: Jobs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs
                .Include(j => j.Company)
                    .ThenInclude(c => c!.Recruiter)
                .Include(j => j.JobApplications)
                    .ThenInclude(ja => ja.Applicant)
                .Include(j => j.JobApplications)
                    .ThenInclude(ja => ja.Interviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(UserRoles.Admin);
            var isRecruiter = User.IsInRole(UserRoles.Recruiter) || User.IsInRole("Company") || User.IsInRole("Recruiter");
            var isJobOwner = isAdmin || (isRecruiter && job.Company != null && job.Company.RecruiterId == currentUserId);

            ViewBag.IsJobOwner = isJobOwner;

            // Determine whether the currently authenticated job seeker has already applied or saved
            var hasApplied = false;
            var hasSaved = false;
            int? userApplicationId = null;
            if (!string.IsNullOrEmpty(currentUserId) && User.IsInRole(UserRoles.JobSeeker))
            {
                var existingApp = await _context.JobApplications
                    .FirstOrDefaultAsync(ja => ja.JobId == job.Id && ja.ApplicantId == currentUserId);
                if (existingApp != null)
                {
                    hasApplied = true;
                    userApplicationId = existingApp.Id;
                }
                hasSaved = await _context.SavedJobs.AnyAsync(sj => sj.JobId == job.Id && sj.ApplicantId == currentUserId);
            }

            ViewData["HasApplied"] = hasApplied;
            ViewData["UserApplicationId"] = userApplicationId;
            ViewData["HasSaved"] = hasSaved;

            // Single placement tracking: identify applicants who are already selected for another job
            if (isJobOwner && job.JobApplications != null && job.JobApplications.Any())
            {
                var applicantIds = job.JobApplications.Select(a => a.ApplicantId).Distinct().ToList();
                var placedCandidates = await _context.JobApplications
                    .Where(ja => applicantIds.Contains(ja.ApplicantId)
                              && ja.JobId != job.Id
                              && ja.Status == ApplicationStatus.Selected)
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

            return View(job);
        }

        // 3. GET: Jobs/Create
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Create()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            if (!User.IsInRole(UserRoles.Admin))
            {
                // Verify recruiter has created a company first
                var userCompany = await _context.Companies.FirstOrDefaultAsync(c => c.RecruiterId == currentUserId);
                if (userCompany == null)
                {
                    TempData["Error"] = "Please create your company profile before posting a job.";
                    return RedirectToAction("Create", "Companies");
                }

                // Company user has their company fixed (no choice needed)
                ViewBag.CompanyName = userCompany.CompanyName;
                ViewBag.UserCompanyId = userCompany.Id;
                ViewData["CompanyId"] = new SelectList(new[] { userCompany }, "Id", "CompanyName", userCompany.Id);
                return View(new Job { CompanyId = userCompany.Id, Company = userCompany });
            }
            else
            {
                ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "CompanyName");
                return View();
            }
        }

        // 4. POST: Jobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Create(Job job)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid();
            }

            Company? targetCompany = null;
            if (!User.IsInRole(UserRoles.Admin))
            {
                targetCompany = await _context.Companies.FirstOrDefaultAsync(c => c.RecruiterId == currentUserId);
                if (targetCompany == null)
                {
                    TempData["Error"] = "Please create your company profile before posting a job.";
                    return RedirectToAction("Create", "Companies");
                }

                // Security: if client attempted to specify another company ID, reject with Forbid
                if (job.CompanyId != 0 && job.CompanyId != targetCompany.Id)
                {
                    return Forbid();
                }

                // Enforce that job belongs to current recruiter's company
                job.CompanyId = targetCompany.Id;
            }
            else
            {
                targetCompany = await _context.Companies.FirstOrDefaultAsync(c => c.Id == job.CompanyId);
                if (targetCompany == null)
                {
                    ModelState.AddModelError("CompanyId", "Selected company was not found.");
                }
            }

            ModelState.Remove("Company");
            ModelState.Remove("JobApplications");
            ModelState.Remove("SavedJobs");

            if (ModelState.IsValid)
            {
                job.CreatedDate = DateTime.UtcNow;
                _context.Add(job);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Job posted successfully.";
                if (User.IsInRole(UserRoles.Admin))
                {
                    return RedirectToAction("Jobs", "Admin");
                }
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole(UserRoles.Admin) && targetCompany != null)
            {
                ViewData["CompanyId"] = new SelectList(new[] { targetCompany }, "Id", "CompanyName", targetCompany.Id);
            }
            else
            {
                ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "CompanyName", job.CompanyId);
            }

            return View(job);
        }

        // 5. GET: Jobs/Edit/5
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

            var job = await _context.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Ownership check: Job -> Company -> RecruiterId == currentUserId
            if (job.Company == null || (job.Company.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin)))
            {
                return Forbid();
            }

            ViewData["CompanyId"] = new SelectList(
                User.IsInRole(UserRoles.Admin)
                    ? _context.Companies
                    : _context.Companies.Where(c => c.RecruiterId == currentUserId),
                "Id", "CompanyName", job.CompanyId);

            return View(job);
        }

        // 6. POST: Jobs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{UserRoles.Recruiter},{UserRoles.Admin}")]
        public async Task<IActionResult> Edit(int id, Job job)
        {
            if (id != job.Id)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid();
            }

            var existingJob = await _context.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (existingJob == null)
            {
                return NotFound();
            }

            // Ownership check: verify existing job in database belongs to recruiter's company
            if (existingJob.Company == null || (existingJob.Company.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin)))
            {
                return Forbid();
            }

            ModelState.Remove("Company");
            ModelState.Remove("JobApplications");
            ModelState.Remove("SavedJobs");

            if (ModelState.IsValid)
            {
                try
                {
                    // Update editable fields while preserving company ownership
                    existingJob.Title = job.Title;
                    existingJob.Description = job.Description;
                    existingJob.Requirements = job.Requirements;
                    existingJob.JobType = job.JobType;
                    existingJob.Location = job.Location;
                    existingJob.Salary = job.Salary;
                    existingJob.LastDate = job.LastDate;
                    existingJob.Status = job.Status;

                    _context.Update(existingJob);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Job updated successfully.";
                    if (User.IsInRole(UserRoles.Admin))
                    {
                        return RedirectToAction("Jobs", "Admin");
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(existingJob.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["CompanyId"] = new SelectList(
                User.IsInRole(UserRoles.Admin)
                    ? _context.Companies
                    : _context.Companies.Where(c => c.RecruiterId == currentUserId),
                "Id", "CompanyName", existingJob.CompanyId);

            return View(job);
        }

        // 7. GET: Jobs/Delete/5
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

            var job = await _context.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Ownership check: Job -> Company -> RecruiterId == currentUserId
            if (job.Company == null || (job.Company.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin)))
            {
                return Forbid();
            }

            return View(job);
        }

        // 8. POST: Jobs/Delete/5
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

            var job = await _context.Jobs
                .Include(j => j.Company)
                .Include(j => j.JobApplications)
                    .ThenInclude(ja => ja.Interviews)
                .Include(j => j.SavedJobs)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Ownership check: Job -> Company -> RecruiterId == currentUserId
            if (job.Company == null || (job.Company.RecruiterId != currentUserId && !User.IsInRole(UserRoles.Admin)))
            {
                return Forbid();
            }

            if (job.SavedJobs != null && job.SavedJobs.Any())
            {
                _context.SavedJobs.RemoveRange(job.SavedJobs);
            }
            if (job.JobApplications != null && job.JobApplications.Any())
            {
                foreach (var app in job.JobApplications)
                {
                    if (app.Interviews != null && app.Interviews.Any())
                    {
                        _context.Interviews.RemoveRange(app.Interviews);
                    }
                }
                _context.JobApplications.RemoveRange(job.JobApplications);
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Job '{job.Title}' deleted successfully.";

            if (User.IsInRole(UserRoles.Admin))
            {
                return RedirectToAction("Jobs", "Admin");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.Id == id);
        }
    }
}

