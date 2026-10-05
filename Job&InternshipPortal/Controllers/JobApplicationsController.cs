using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
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
            }

            if (ModelState.IsValid)
            {
                _context.Add(jobApplication);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", jobApplication.ApplicantId);
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", jobApplication.JobId);
            return View(jobApplication);
        }

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

            ModelState.Remove("Job");
            ModelState.Remove("Applicant");
            ModelState.Remove("Interviews");

            if (ModelState.IsValid)
            {
                try
                {
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

            return View(jobApplication);
        }

        // 8. POST: JobApplications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
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
    }
}
