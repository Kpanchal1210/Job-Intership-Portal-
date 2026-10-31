using System.Diagnostics;
using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Job_InternshipPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                FeaturedJobs = await _context.Jobs
                    .Include(j => j.Company)
                    .Where(j => j.Status == JobStatus.Approved)
                    .OrderByDescending(j => j.CreatedDate)
                    .Take(6)
                    .ToListAsync(),
                FeaturedCompanies = await _context.Companies
                    .Include(c => c.Jobs)
                    .Take(6)
                    .ToListAsync(),
                TotalJobs = await _context.Jobs.CountAsync(j => j.Status == JobStatus.Approved),
                TotalInternships = await _context.Jobs.CountAsync(j => j.Status == JobStatus.Approved && j.JobType == JobType.Internship),
                TotalCompanies = await _context.Companies.CountAsync(),
                TotalApplications = await _context.JobApplications.CountAsync()
            };

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserId) && User.IsInRole(UserRoles.JobSeeker))
            {
                var appliedJobMap = await _context.JobApplications
                    .Where(a => a.ApplicantId == currentUserId)
                    .ToDictionaryAsync(a => a.JobId, a => a.Id);
                ViewBag.AppliedJobMap = appliedJobMap;
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
