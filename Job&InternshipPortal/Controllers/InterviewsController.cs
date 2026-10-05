using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
    // ========================================================
    // 2-Person Project Division: Person 2
    // Module: Interview CRUD
    // ========================================================
    public class InterviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InterviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: Interviews
        public async Task<IActionResult> Index()
        {
            var interviews = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Applicant)
                .ToListAsync();

            return View(interviews);
        }

        // 2. GET: Interviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (interview == null)
            {
                return NotFound();
            }

            return View(interview);
        }

        // 3. GET: Interviews/Create
        public IActionResult Create()
        {
            PopulateApplicationsDropDownList();
            return View();
        }

        // 4. POST: Interviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Interview interview)
        {
            ModelState.Remove("JobApplication");

            if (ModelState.IsValid)
            {
                _context.Add(interview);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateApplicationsDropDownList(interview.JobApplicationId);
            return View(interview);
        }

        // 5. GET: Interviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null)
            {
                return NotFound();
            }

            PopulateApplicationsDropDownList(interview.JobApplicationId);
            return View(interview);
        }

        // 6. POST: Interviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Interview interview)
        {
            if (id != interview.Id)
            {
                return NotFound();
            }

            ModelState.Remove("JobApplication");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(interview);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InterviewExists(interview.Id))
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
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Job)
                .Include(i => i.JobApplication)
                    .ThenInclude(ja => ja!.Applicant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (interview == null)
            {
                return NotFound();
            }

            return View(interview);
        }

        // 8. POST: Interviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var interview = await _context.Interviews.FindAsync(id);
            if (interview != null)
            {
                _context.Interviews.Remove(interview);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private void PopulateApplicationsDropDownList(object? selectedApplication = null)
        {
            var applications = _context.JobApplications
                .Include(ja => ja.Job)
                .Select(ja => new
                {
                    ja.Id,
                    Display = "Application #" + ja.Id + " (" + (ja.Job != null ? ja.Job.Title : "Job") + ")"
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
