using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
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
        }

        // 1. GET: SavedJobs
        public async Task<IActionResult> Index()
        {
            var savedJobs = await _context.SavedJobs
                .Include(s => s.Job)
                .Include(s => s.Applicant)
                .ToListAsync();

            return View(savedJobs);
        }

        // 2. GET: SavedJobs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var savedJob = await _context.SavedJobs
                .Include(s => s.Job)
                .Include(s => s.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (savedJob == null)
            {
                return NotFound();
            }

            return View(savedJob);
        }

        // 3. GET: SavedJobs/Create
        public IActionResult Create()
        {
            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName");
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title");
            return View();
        }

        // 4. POST: SavedJobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SavedJob savedJob)
        {
            ModelState.Remove("Job");
            ModelState.Remove("Applicant");

            if (await _context.SavedJobs.AnyAsync(s => s.JobId == savedJob.JobId && s.ApplicantId == savedJob.ApplicantId))
            {
                ModelState.AddModelError(string.Empty, "This job has already been saved by this applicant.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(savedJob);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ApplicantId"] = new SelectList(_context.Users, "Id", "UserName", savedJob.ApplicantId);
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Title", savedJob.JobId);
            return View(savedJob);
        }

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
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var savedJob = await _context.SavedJobs
                .Include(s => s.Job)
                .Include(s => s.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (savedJob == null)
            {
                return NotFound();
            }

            return View(savedJob);
        }

        // 8. POST: SavedJobs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var savedJob = await _context.SavedJobs.FindAsync(id);
            if (savedJob != null)
            {
                _context.SavedJobs.Remove(savedJob);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SavedJobExists(int id)
        {
            return _context.SavedJobs.Any(e => e.Id == id);
        }
    }
}
