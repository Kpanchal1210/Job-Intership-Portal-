using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
<<<<<<< HEAD
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
<<<<<<< HEAD
    // Module: Saved Job CRUD (Secured: Only Authenticated Job Seekers)
    [Authorize(Roles = $"{UserRoles.JobSeeker},{UserRoles.Admin}")]
    public class SavedJobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SavedJobsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
=======
    // ========================================================
    // 2-Person Project Division: Person 2
    // Module: Saved Job CRUD
    // ========================================================
    public class SavedJobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SavedJobsController(ApplicationDbContext context)
        {
            _context = context;
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
        }

        // 1. GET: SavedJobs
        public async Task<IActionResult> Index()
        {
<<<<<<< HEAD
            var currentUserId = _userManager.GetUserId(User);
            var query = _context.SavedJobs
                .Include(s => s.Job)
                    .ThenInclude(j => j!.Company)
                .Include(s => s.Applicant)
                .AsQueryable();

            if (!User.IsInRole(UserRoles.Admin))
            {
                query = query.Where(s => s.ApplicantId == currentUserId);
            }

            var savedJobs = await query.OrderByDescending(s => s.SavedDate).ToListAsync();
=======
            var savedJobs = await _context.SavedJobs
                .Include(s => s.Job)
                .Include(s => s.Applicant)
                .ToListAsync();

>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            return View(savedJobs);
        }

        // 2. GET: SavedJobs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

<<<<<<< HEAD
            var currentUserId = _userManager.GetUserId(User);
            var savedJob = await _context.SavedJobs
                .Include(s => s.Job)
                    .ThenInclude(j => j!.Company)
=======
            var savedJob = await _context.SavedJobs
                .Include(s => s.Job)
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
                .Include(s => s.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (savedJob == null)
            {
                return NotFound();
            }

<<<<<<< HEAD
            if (savedJob.ApplicantId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            return View(savedJob);
        }

        // 3. GET: SavedJobs/Create
<<<<<<< HEAD
        public async Task<IActionResult> Create(int? jobId = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            if (jobId.HasValue && await _context.SavedJobs.AnyAsync(s => s.JobId == jobId.Value && s.ApplicantId == currentUserId))
            {
                TempData["Error"] = "You have already saved this job.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", jobId);

            var savedJob = new SavedJob
            {
                JobId = jobId ?? 0,
                ApplicantId = currentUserId,
                SavedDate = DateTime.UtcNow
            };

            if (jobId.HasValue && jobId.Value > 0)
            {
                savedJob.Job = await _context.Jobs
                    .Include(j => j.Company)
                    .FirstOrDefaultAsync(j => j.Id == jobId.Value);
            }

            return View(savedJob);
=======
        public IActionResult Create()
        {
            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName");
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title");
            return View();
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
        }

        // 4. POST: SavedJobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SavedJob savedJob)
        {
<<<<<<< HEAD
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid();
            }

            // Always enforce current user as the applicant
            savedJob.ApplicantId = currentUserId;
            savedJob.SavedDate = DateTime.UtcNow;

            ModelState.Remove("Job");
            ModelState.Remove("Applicant");
            ModelState.Remove("ApplicantId");

            if (await _context.SavedJobs.AnyAsync(s => s.JobId == savedJob.JobId && s.ApplicantId == currentUserId))
            {
                ModelState.AddModelError(string.Empty, "This job has already been saved to your list.");
=======
            ModelState.Remove("Job");
            ModelState.Remove("Applicant");

            if (await _context.SavedJobs.AnyAsync(s => s.JobId == savedJob.JobId && s.ApplicantId == savedJob.ApplicantId))
            {
                ModelState.AddModelError(string.Empty, "This job has already been saved by this applicant.");
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            }

            if (ModelState.IsValid)
            {
                _context.Add(savedJob);
                await _context.SaveChangesAsync();
<<<<<<< HEAD
                TempData["Success"] = "Job saved successfully!";
                return RedirectToAction(nameof(Index));
            }

=======
                return RedirectToAction(nameof(Index));
            }

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", savedJob.ApplicantId);
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", savedJob.JobId);
            return View(savedJob);
        }

<<<<<<< HEAD
        // 5. GET: SavedJobs/Delete/5
=======
        // 5. GET: SavedJobs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var savedJob = await _context.SavedJobs.FindAsync(id);
            if (savedJob == null)
            {
                return NotFound();
            }

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", savedJob.ApplicantId);
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", savedJob.JobId);
            return View(savedJob);
        }

        // 6. POST: SavedJobs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SavedJob savedJob)
        {
            if (id != savedJob.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Job");
            ModelState.Remove("Applicant");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(savedJob);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SavedJobExists(savedJob.Id))
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

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", savedJob.ApplicantId);
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", savedJob.JobId);
            return View(savedJob);
        }

        // 7. GET: SavedJobs/Delete/5
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

<<<<<<< HEAD
            var currentUserId = _userManager.GetUserId(User);
            var savedJob = await _context.SavedJobs
                .Include(s => s.Job)
                    .ThenInclude(j => j!.Company)
=======
            var savedJob = await _context.SavedJobs
                .Include(s => s.Job)
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
                .Include(s => s.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (savedJob == null)
            {
                return NotFound();
            }

<<<<<<< HEAD
            if (savedJob.ApplicantId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            return View(savedJob);
        }

        // 6. POST: SavedJobs/Delete/5
=======
            return View(savedJob);
        }

        // 8. POST: SavedJobs/Delete/5
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
<<<<<<< HEAD
            var currentUserId = _userManager.GetUserId(User);
            var savedJob = await _context.SavedJobs.FindAsync(id);
            if (savedJob == null)
            {
                return NotFound();
            }

            if (savedJob.ApplicantId != currentUserId && !User.IsInRole(UserRoles.Admin))
            {
                return Forbid();
            }

            _context.SavedJobs.Remove(savedJob);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Saved job removed.";

=======
            var savedJob = await _context.SavedJobs.FindAsync(id);
            if (savedJob != null)
            {
                _context.SavedJobs.Remove(savedJob);
                await _context.SaveChangesAsync();
            }

>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            return RedirectToAction(nameof(Index));
        }

        private bool SavedJobExists(int id)
        {
            return _context.SavedJobs.Any(e => e.Id == id);
        }
    }
}
<<<<<<< HEAD

=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
