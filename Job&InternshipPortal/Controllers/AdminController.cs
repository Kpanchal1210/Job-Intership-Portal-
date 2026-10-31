using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Job_InternshipPortal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalCompanies = await _context.Companies.CountAsync(),
                TotalJobs = await _context.Jobs.CountAsync(),
                TotalApplications = await _context.JobApplications.CountAsync(),
                PendingJobs = await _context.Jobs.CountAsync(j => j.Status == JobStatus.Pending)
            };

            return View(viewModel);
        }

        // 2. GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            var currentUserId = _userManager.GetUserId(User);
            var users = await _context.Users.OrderBy(u => u.FullName).ToListAsync();

            var userViewModels = new List<UserViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Roles = roles,
                    IsCurrentAdmin = user.Id == currentUserId
                });
            }

            return View(userViewModels);
        }

        // 3. GET: /Admin/UserDetails/5
        public async Task<IActionResult> UserDetails(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Company)
                    .ThenInclude(c => c!.Jobs)
                .Include(u => u.JobApplications)
                    .ThenInclude(ja => ja.Job)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            return View(user);
        }

        // 4. POST: /Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                TempData["Error"] = "Action Denied: You cannot delete your own currently logged-in administrator account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.Equals(user.Email, "admin@jobportal.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Action Denied: The primary system administrator account cannot be deleted.";
                return RedirectToAction(nameof(Users));
            }

            // Remove associated user data safely to maintain DB integrity
            var savedJobs = await _context.SavedJobs.Where(sj => sj.ApplicantId == id).ToListAsync();
            if (savedJobs.Any())
            {
                _context.SavedJobs.RemoveRange(savedJobs);
            }

            var applications = await _context.JobApplications
                .Include(ja => ja.Interviews)
                .Where(ja => ja.ApplicantId == id)
                .ToListAsync();

            if (applications.Any())
            {
                foreach (var app in applications)
                {
                    if (app.Interviews.Any())
                    {
                        _context.Interviews.RemoveRange(app.Interviews);
                    }
                }
                _context.JobApplications.RemoveRange(applications);
            }

            var company = await _context.Companies
                .Include(c => c.Jobs)
                    .ThenInclude(j => j.JobApplications)
                        .ThenInclude(ja => ja.Interviews)
                .Include(c => c.Jobs)
                    .ThenInclude(j => j.SavedJobs)
                .FirstOrDefaultAsync(c => c.RecruiterId == id);

            if (company != null)
            {
                foreach (var job in company.Jobs)
                {
                    if (job.SavedJobs != null && job.SavedJobs.Any())
                    {
                        _context.SavedJobs.RemoveRange(job.SavedJobs);
                    }
                    foreach (var app in job.JobApplications)
                    {
                        if (app.Interviews.Any())
                        {
                            _context.Interviews.RemoveRange(app.Interviews);
                        }
                    }
                    _context.JobApplications.RemoveRange(job.JobApplications);
                }
                _context.Jobs.RemoveRange(company.Jobs);
                _context.Companies.Remove(company);
            }

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"User '{user.FullName}' ({user.Email}) was deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }

        // 5. GET: /Admin/Companies
        public async Task<IActionResult> Companies()
        {
            var companies = await _context.Companies
                .Include(c => c.Recruiter)
                .Include(c => c.Jobs)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            return View(companies);
        }

        // 6. GET: /Admin/Jobs
        public async Task<IActionResult> Jobs()
        {
            var jobs = await _context.Jobs
                .Include(j => j.Company)
                    .ThenInclude(c => c!.Recruiter)
                .OrderByDescending(j => j.CreatedDate)
                .ToListAsync();

            return View(jobs);
        }

        // 7. GET: /Admin/Applications
        public async Task<IActionResult> Applications()
        {
            var applications = await _context.JobApplications
                .Include(ja => ja.Applicant)
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Include(ja => ja.Interviews)
                .OrderByDescending(ja => ja.AppliedDate)
                .ToListAsync();

            return View(applications);
        }

        // 8. POST: /Admin/ApproveJob/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            job.Status = JobStatus.Approved;
            if (job.LastDate < DateTime.UtcNow.Date)
            {
                job.LastDate = DateTime.UtcNow.Date.AddDays(30);
            }
            _context.Update(job);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Job '{job.Title}' has been approved and is now visible to job seekers.";
            return RedirectToAction(nameof(Jobs));
        }

        // 9. POST: /Admin/RejectJob/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            job.Status = JobStatus.Rejected;
            _context.Update(job);
            await _context.SaveChangesAsync();

            TempData["Error"] = $"Job '{job.Title}' has been rejected.";
            return RedirectToAction(nameof(Jobs));
        }

        // 10. POST: /Admin/CloseJob/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            job.Status = JobStatus.Closed;
            _context.Update(job);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Job '{job.Title}' has been closed.";
            return RedirectToAction(nameof(Jobs));
        }
    }
}
