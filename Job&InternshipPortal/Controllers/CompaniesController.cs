using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
    // ========================================================
    // 2-Person Project Division: Person 1
    // Module: Company CRUD
    // ========================================================
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompaniesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: Companies
        public async Task<IActionResult> Index()
        {
            var companies = await _context.Companies
                .Include(c => c.Recruiter)
                .ToListAsync();

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
        public IActionResult Create()
        {
            ViewData["RecruiterId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        // 4. POST: Companies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company company)
        {
            ModelState.Remove("Recruiter");
            ModelState.Remove("Jobs");

            if (ModelState.IsValid)
            {
                _context.Add(company);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["RecruiterId"] = new SelectList(_context.Users, "Id", "UserName", company.RecruiterId);
            return View(company);
        }

        // 5. GET: Companies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            ViewData["RecruiterId"] = new SelectList(_context.Users, "Id", "UserName", company.RecruiterId);
            return View(company);
        }

        // 6. POST: Companies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company company)
        {
            if (id != company.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Recruiter");
            ModelState.Remove("Jobs");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(company);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompanyExists(company.Id))
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

            ViewData["RecruiterId"] = new SelectList(_context.Users, "Id", "UserName", company.RecruiterId);
            return View(company);
        }

        // 7. GET: Companies/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // 8. POST: Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company != null)
            {
                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CompanyExists(int id)
        {
            return _context.Companies.Any(e => e.Id == id);
        }
    }
}
