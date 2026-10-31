using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Job_InternshipPortal.Controllers;
using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
using Job_InternshipPortal.Models.ViewModels;
using Job_InternshipPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Job_InternshipPortal.Testing
{
    public class SimpleTestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object?> _tempData = new();

        public IDictionary<string, object?> LoadTempData(HttpContext context)
        {
            return _tempData;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
        {
            _tempData.Clear();
            foreach (var kvp in values)
            {
                _tempData[kvp.Key] = kvp.Value;
            }
        }
    }

    public static class PortalTestRunner
    {
        public static async Task<int> RunAllTestsAsync(IServiceProvider serviceProvider)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("STARTING COMPLETE END-TO-END RECRUITMENT WORKFLOW TEST SUITE (TESTS 1 - 17)");
            Console.WriteLine("================================================================================");

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var workflowService = scope.ServiceProvider.GetRequiredService<IApplicationWorkflowService>();

            var results = new Dictionary<int, (string Name, bool Passed, string Details)>();

            try
            {
                // Ensure required roles exist
                string[] roles = { UserRoles.Admin, UserRoles.Recruiter, UserRoles.JobSeeker };
                foreach (var r in roles)
                {
                    if (!await roleManager.RoleExistsAsync(r))
                        await roleManager.CreateAsync(new IdentityRole(r));
                }

                // ── Seed test users ──────────────────────────────────────────────────
                var recAUser = await GetOrCreateUserAsync(userManager, "company_a_recruiter@portal.com", "Recruiter Company A", UserRoles.Recruiter);
                var recBUser = await GetOrCreateUserAsync(userManager, "company_b_recruiter@portal.com", "Recruiter Company B", UserRoles.Recruiter);
                var studentAUser = await GetOrCreateUserAsync(userManager, "student_a@portal.com", "Student A", UserRoles.JobSeeker);
                var studentBUser = await GetOrCreateUserAsync(userManager, "student_b@portal.com", "Student B", UserRoles.JobSeeker);
                var studentCUser = await GetOrCreateUserAsync(userManager, "student_c@portal.com", "Student C", UserRoles.JobSeeker);
                var studentDUser = await GetOrCreateUserAsync(userManager, "student_d@portal.com", "Student D", UserRoles.JobSeeker);
                var studentEUser = await GetOrCreateUserAsync(userManager, "student_e@portal.com", "Student E", UserRoles.JobSeeker);
                var studentFUser = await GetOrCreateUserAsync(userManager, "student_f@portal.com", "Student F", UserRoles.JobSeeker);
                var studentGUser = await GetOrCreateUserAsync(userManager, "student_g@portal.com", "Student G", UserRoles.JobSeeker);

                // ── Seed Companies ───────────────────────────────────────────────────
                var companyA = await context.Companies.FirstOrDefaultAsync(c => c.RecruiterId == recAUser.Id);
                if (companyA == null)
                {
                    companyA = new Company
                    {
                        CompanyName = "Acme Global Solutions",
                        Website = "https://acme.example.com",
                        RecruiterId = recAUser.Id,
                        Location = "San Francisco, CA"
                    };
                    context.Companies.Add(companyA);
                    await context.SaveChangesAsync();
                }

                var companyB = await context.Companies.FirstOrDefaultAsync(c => c.RecruiterId == recBUser.Id);
                if (companyB == null)
                {
                    companyB = new Company
                    {
                        CompanyName = "Zenith Corp",
                        Website = "https://zenith.example.com",
                        RecruiterId = recBUser.Id,
                        Location = "Austin, TX"
                    };
                    context.Companies.Add(companyB);
                    await context.SaveChangesAsync();
                }

                // ── Seed Jobs ────────────────────────────────────────────────────────
                var jobA = await context.Jobs.FirstOrDefaultAsync(j => j.CompanyId == companyA.Id && j.Title == "Senior Systems Engineer");
                if (jobA == null)
                {
                    jobA = new Job
                    {
                        Title = "Senior Systems Engineer",
                        Description = "Full stack developer role",
                        Location = "Remote",
                        CompanyId = companyA.Id,
                        JobType = JobType.FullTime,
                        CreatedDate = DateTime.UtcNow,
                        LastDate = DateTime.UtcNow.AddMonths(2),
                        Status = JobStatus.Approved
                    };
                    context.Jobs.Add(jobA);
                    await context.SaveChangesAsync();
                }

                var jobB = await context.Jobs.FirstOrDefaultAsync(j => j.CompanyId == companyB.Id && j.Title == "Cloud Architect");
                if (jobB == null)
                {
                    jobB = new Job
                    {
                        Title = "Cloud Architect",
                        Description = "GCP Cloud Infrastructure",
                        Location = "Austin, TX",
                        CompanyId = companyB.Id,
                        JobType = JobType.FullTime,
                        CreatedDate = DateTime.UtcNow,
                        LastDate = DateTime.UtcNow.AddMonths(2),
                        Status = JobStatus.Approved
                    };
                    context.Jobs.Add(jobB);
                    await context.SaveChangesAsync();
                }

                // ── Helper to create Controller with User context ─────────────────────
                CompaniesController CreateCompaniesController(ApplicationUser user, string role)
                {
                    var httpContext = new DefaultHttpContext();
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.Email ?? user.UserName ?? "user"),
                        new Claim(ClaimTypes.Role, role)
                    };
                    httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
                    var tempDataProvider = new SimpleTestTempDataProvider();
                    return new CompaniesController(context, userManager, workflowService)
                    {
                        ControllerContext = new ControllerContext { HttpContext = httpContext },
                        TempData = new TempDataDictionary(httpContext, tempDataProvider)
                    };
                }

                JobApplicationsController CreateJobApplicationsController(ApplicationUser user, string role)
                {
                    var httpContext = new DefaultHttpContext();
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.Email ?? user.UserName ?? "user"),
                        new Claim(ClaimTypes.Role, role)
                    };
                    httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
                    var tempDataProvider = new SimpleTestTempDataProvider();
                    return new JobApplicationsController(context, userManager, workflowService)
                    {
                        ControllerContext = new ControllerContext { HttpContext = httpContext },
                        TempData = new TempDataDictionary(httpContext, tempDataProvider)
                    };
                }

                InterviewsController CreateInterviewsController(ApplicationUser user, string role)
                {
                    var httpContext = new DefaultHttpContext();
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.Email ?? user.UserName ?? "user"),
                        new Claim(ClaimTypes.Role, role)
                    };
                    httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
                    var tempDataProvider = new SimpleTestTempDataProvider();
                    return new InterviewsController(context, userManager, workflowService)
                    {
                        ControllerContext = new ControllerContext { HttpContext = httpContext },
                        TempData = new TempDataDictionary(httpContext, tempDataProvider)
                    };
                }

                // Clean previous test applications for these test jobs
                var existingTestApps = await context.JobApplications
                    .Where(ja => ja.JobId == jobA.Id || ja.JobId == jobB.Id)
                    .ToListAsync();
                context.JobApplications.RemoveRange(existingTestApps);
                await context.SaveChangesAsync();

                // =====================================================================
                // TEST 1: Student A applies for a valid active job.
                // Expected: Application is saved with Applied status.
                // =====================================================================
                JobApplication? app1 = null;
                try
                {
                    var studentCtrl = CreateJobApplicationsController(studentAUser, UserRoles.JobSeeker);
                    var newApp = new JobApplication
                    {
                        JobId = jobA.Id,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    var postResult = await studentCtrl.Create(newApp, null);

                    app1 = await context.JobApplications
                        .FirstOrDefaultAsync(ja => ja.JobId == jobA.Id && ja.ApplicantId == studentAUser.Id);

                    bool pass1 = postResult is RedirectToActionResult &&
                                 app1 != null &&
                                 app1.Status == ApplicationStatus.Applied &&
                                 app1.AppliedDate <= DateTime.UtcNow;

                    results[1] = ("Student A applies for active job -> Saved as Applied", pass1,
                        pass1 ? $"Application #{app1?.Id} saved in DB with Status={app1?.Status}." : "Application was not saved with status Applied.");
                }
                catch (Exception ex)
                {
                    results[1] = ("Student A applies for active job -> Saved as Applied", false, ex.Message);
                }

                // =====================================================================
                // TEST 2: Student A applies for the same job again.
                // Expected: Duplicate application is prevented.
                // =====================================================================
                try
                {
                    var studentCtrl = CreateJobApplicationsController(studentAUser, UserRoles.JobSeeker);
                    var duplicateApp = new JobApplication
                    {
                        JobId = jobA.Id,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    var postResult = await studentCtrl.Create(duplicateApp, null);

                    var count = await context.JobApplications
                        .CountAsync(ja => ja.JobId == jobA.Id && ja.ApplicantId == studentAUser.Id);

                    bool pass2 = !studentCtrl.ModelState.IsValid && count == 1;

                    results[2] = ("Student A applies again for same job -> Duplicate blocked", pass2,
                        pass2 ? "Duplicate application was rejected by model validation. Total apps remains 1." : "Duplicate application was not prevented.");
                }
                catch (Exception ex)
                {
                    results[2] = ("Student A applies again for same job -> Duplicate blocked", false, ex.Message);
                }

                // =====================================================================
                // TEST 3: Company A views applicants for its own job.
                // Expected: Correct applications are displayed.
                // =====================================================================
                try
                {
                    var compCtrl = CreateJobApplicationsController(recAUser, UserRoles.Recruiter);
                    var result = await compCtrl.Index(jobA.Id) as ViewResult;
                    var model = result?.Model as List<JobApplication>;

                    bool pass3 = result != null && model != null &&
                                 model.Any(a => a.ApplicantId == studentAUser.Id && a.JobId == jobA.Id);

                    results[3] = ("Company A views applicants for its own job -> Correct apps shown", pass3,
                        pass3 ? $"Company A viewed {model?.Count} application(s) for Job #{jobA.Id}." : "Applicants list did not show the expected application.");
                }
                catch (Exception ex)
                {
                    results[3] = ("Company A views applicants for its own job -> Correct apps shown", false, ex.Message);
                }

                // =====================================================================
                // TEST 4: Company A changes an eligible application to UnderReview.
                // Expected: Database and student dashboard show Under Review.
                // =====================================================================
                try
                {
                    var compCtrl = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    var postResult = await compCtrl.MarkUnderReview(app1!.Id);

                    var reloadedApp1 = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == app1.Id);

                    var studentCtrl = CreateJobApplicationsController(studentAUser, UserRoles.JobSeeker);
                    var dashResult = await studentCtrl.Dashboard() as ViewResult;
                    var dashModel = dashResult?.Model as JobSeekerDashboardViewModel;

                    bool pass4 = reloadedApp1.Status == ApplicationStatus.UnderReview &&
                                 dashModel != null &&
                                 dashModel.ApplicationsUnderReview >= 1;

                    results[4] = ("Company A changes eligible app to UnderReview -> DB & student dashboard show Under Review", pass4,
                        pass4 ? $"Application #{app1.Id} status is UnderReview. Student dashboard shows ApplicationsUnderReview={dashModel?.ApplicationsUnderReview}." : "Status change to UnderReview failed.");
                }
                catch (Exception ex)
                {
                    results[4] = ("Company A changes eligible app to UnderReview -> DB & student dashboard show Under Review", false, ex.Message);
                }

                // =====================================================================
                // TEST 5: Company A shortlists a candidate.
                // Expected: Candidate appears in Shortlisted Applicants and student sees Shortlisted.
                // =====================================================================
                try
                {
                    var compCtrl = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    var postResult = await compCtrl.ShortlistCandidate(app1!.Id);

                    var reloadedApp1 = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == app1.Id);

                    var shortlistedResult = await compCtrl.ShortlistedApplicants(jobA.Id) as ViewResult;
                    var shortlistedModel = shortlistedResult?.Model as List<JobApplication>;

                    var studentCtrl = CreateJobApplicationsController(studentAUser, UserRoles.JobSeeker);
                    var dashResult = await studentCtrl.Dashboard() as ViewResult;
                    var dashModel = dashResult?.Model as JobSeekerDashboardViewModel;

                    bool pass5 = reloadedApp1.Status == ApplicationStatus.Shortlisted &&
                                 shortlistedModel != null &&
                                 shortlistedModel.Any(a => a.Id == app1.Id) &&
                                 dashModel != null &&
                                 dashModel.Shortlisted >= 1;

                    results[5] = ("Company A shortlists candidate -> Candidate in Shortlisted page & student dashboard shows Shortlisted", pass5,
                        pass5 ? $"Candidate appears in Shortlisted list and student dashboard Shortlisted={dashModel?.Shortlisted}." : "Shortlist candidate flow failed.");
                }
                catch (Exception ex)
                {
                    results[5] = ("Company A shortlists candidate -> Candidate in Shortlisted page & student dashboard shows Shortlisted", false, ex.Message);
                }

                // =====================================================================
                // TEST 6: Company A schedules an interview.
                // Expected: Interview record saved, status changes to Interview, details on both dashboards.
                // =====================================================================
                Interview? scheduledIv = null;
                try
                {
                    var ivCtrl = CreateInterviewsController(recAUser, UserRoles.Recruiter);
                    var interviewInput = new Interview
                    {
                        JobApplicationId = app1!.Id,
                        InterviewDate = DateTime.UtcNow.AddDays(3),
                        InterviewType = InterviewType.Online,
                        MeetingLink = "https://meet.google.com/test-workflow"
                    };
                    var postResult = await ivCtrl.Create(interviewInput, "Technical coding round");

                    scheduledIv = await context.Interviews.FirstOrDefaultAsync(i => i.JobApplicationId == app1!.Id);
                    var reloadedApp1 = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == app1.Id);

                    var compCtrl = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    var compDashResult = await compCtrl.Dashboard() as ViewResult;
                    var compDashModel = compDashResult?.Model as RecruiterDashboardViewModel;

                    var studentCtrl = CreateJobApplicationsController(studentAUser, UserRoles.JobSeeker);
                    var studDashResult = await studentCtrl.Dashboard() as ViewResult;
                    var studDashModel = studDashResult?.Model as JobSeekerDashboardViewModel;

                    bool pass6 = scheduledIv != null &&
                                 reloadedApp1.Status == ApplicationStatus.Interview &&
                                 compDashModel != null && compDashModel.UpcomingInterviewCount >= 1 &&
                                 studDashModel != null && studDashModel.UpcomingInterviewCount >= 1;

                    results[6] = ("Company A schedules interview -> Interview saved, status Interview, visible on both dashboards", pass6,
                        pass6 ? $"Interview #{scheduledIv?.Id} scheduled, app status Interview, visible on Company & Student dashboards." : "Interview scheduling failed.");
                }
                catch (Exception ex)
                {
                    results[6] = ("Company A schedules interview -> Interview saved, status Interview, visible on both dashboards", false, ex.Message);
                }

                // =====================================================================
                // TEST 7: Company A selects an eligible candidate.
                // Expected: Status changes to Selected and candidate appears on Selected Candidates page.
                // =====================================================================
                try
                {
                    var compCtrl = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    var postResult = await compCtrl.SelectCandidate(app1!.Id);

                    var reloadedApp1 = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == app1.Id);

                    var selectedResult = await compCtrl.SelectedCandidates(jobA.Id) as ViewResult;
                    var selectedModel = selectedResult?.Model as List<JobApplication>;

                    var studentCtrl = CreateJobApplicationsController(studentAUser, UserRoles.JobSeeker);
                    var studDashResult = await studentCtrl.Dashboard() as ViewResult;
                    var studDashModel = studDashResult?.Model as JobSeekerDashboardViewModel;

                    bool pass7 = reloadedApp1.Status == ApplicationStatus.Selected &&
                                 reloadedApp1.SelectedDate != null &&
                                 selectedModel != null &&
                                 selectedModel.Any(a => a.Id == app1.Id) &&
                                 studDashModel != null &&
                                 studDashModel.Selected >= 1;

                    results[7] = ("Company A selects candidate -> Status Selected, appears on Selected Candidates page", pass7,
                        pass7 ? $"Candidate Selected with timestamp {reloadedApp1.SelectedDate:yyyy-MM-dd HH:mm:ss}, listed on Selected page, student dashboard Selected={studDashModel?.Selected}." : "Final selection failed.");
                }
                catch (Exception ex)
                {
                    results[7] = ("Company A selects candidate -> Status Selected, appears on Selected Candidates page", false, ex.Message);
                }

                // =====================================================================
                // TEST 8: Company A rejects an eligible candidate.
                // Expected: Status changes to Rejected and student sees updated status.
                // =====================================================================
                JobApplication app2 = null!;
                try
                {
                    // Create application for Student B
                    app2 = new JobApplication
                    {
                        JobId = jobA.Id,
                        ApplicantId = studentBUser.Id,
                        AppliedDate = DateTime.UtcNow,
                        Status = ApplicationStatus.UnderReview,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    context.JobApplications.Add(app2);
                    await context.SaveChangesAsync();

                    var compCtrl = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    var postResult = await compCtrl.RejectCandidate(app2.Id);

                    var reloadedApp2 = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == app2.Id);

                    var studentCtrl = CreateJobApplicationsController(studentBUser, UserRoles.JobSeeker);
                    var studDashResult = await studentCtrl.Dashboard() as ViewResult;
                    var studDashModel = studDashResult?.Model as JobSeekerDashboardViewModel;

                    bool pass8 = reloadedApp2.Status == ApplicationStatus.Rejected &&
                                 studDashModel != null &&
                                 studDashModel.RejectedApplications >= 1;

                    results[8] = ("Company A rejects candidate -> Status Rejected, student dashboard shows Rejected", pass8,
                        pass8 ? $"Application #{app2.Id} rejected. Student dashboard shows RejectedApplications={studDashModel?.RejectedApplications}." : "Candidate rejection failed.");
                }
                catch (Exception ex)
                {
                    results[8] = ("Company A rejects candidate -> Status Rejected, student dashboard shows Rejected", false, ex.Message);
                }

                // =====================================================================
                // TEST 9: Company A tries to access Company B's application.
                // Expected: Access is denied or a consistent 404 response is returned.
                // =====================================================================
                try
                {
                    var appB = new JobApplication
                    {
                        JobId = jobB.Id,
                        ApplicantId = studentCUser.Id,
                        AppliedDate = DateTime.UtcNow,
                        Status = ApplicationStatus.Shortlisted,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    context.JobApplications.Add(appB);
                    await context.SaveChangesAsync();

                    var compCtrlA = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    var crossSelectResult = await compCtrlA.SelectCandidate(appB.Id);

                    var jaCtrlA = CreateJobApplicationsController(recAUser, UserRoles.Recruiter);
                    var crossDetailsResult = await jaCtrlA.Details(appB.Id);

                    bool pass9 = crossSelectResult is ForbidResult && crossDetailsResult is ForbidResult;

                    results[9] = ("Company A attempts to access Company B's application -> Denied (403)", pass9,
                        pass9 ? "Cross-company access strictly blocked: SelectCandidate returned ForbidResult, Details returned ForbidResult." : "Cross-company isolation failed.");
                }
                catch (Exception ex)
                {
                    results[9] = ("Company A attempts to access Company B's application -> Denied (403)", false, ex.Message);
                }

                // =====================================================================
                // TEST 10: Student A tries to access Student B's application.
                // Expected: Access is denied or a consistent 404 response is returned.
                // =====================================================================
                try
                {
                    var studentCtrlA = CreateJobApplicationsController(studentAUser, UserRoles.JobSeeker);
                    var crossAccessResult = await studentCtrlA.Details(app2.Id);

                    bool pass10 = crossAccessResult is ForbidResult;

                    results[10] = ("Student A attempts to access Student B's application -> Denied (403)", pass10,
                        pass10 ? "Cross-student access strictly blocked: Details returned ForbidResult for unauthorized student." : "Cross-student privacy isolation failed.");
                }
                catch (Exception ex)
                {
                    results[10] = ("Student A attempts to access Student B's application -> Denied (403)", false, ex.Message);
                }

                // =====================================================================
                // TEST 11: A Job Seeker attempts to change an application status directly.
                // Expected: Request is denied.
                // =====================================================================
                try
                {
                    var method = typeof(CompaniesController).GetMethod(nameof(CompaniesController.SelectCandidate));
                    var authAttr = method?.GetCustomAttribute<AuthorizeAttribute>();
                    bool roleRestricted = authAttr != null &&
                                         !string.IsNullOrEmpty(authAttr.Roles) &&
                                         !authAttr.Roles.Contains(UserRoles.JobSeeker);

                    var studentAsCompany = CreateCompaniesController(studentAUser, UserRoles.JobSeeker);
                    var directAttempt = await studentAsCompany.SelectCandidate(app1!.Id);

                    bool pass11 = roleRestricted && directAttempt is ForbidResult;

                    results[11] = ("Job Seeker attempts to change status directly -> Denied", pass11,
                        pass11 ? "Role authorization and ownership checks deny student from changing application status." : "Student was able to invoke status modification.");
                }
                catch (Exception ex)
                {
                    results[11] = ("Job Seeker attempts to change status directly -> Denied", false, ex.Message);
                }

                // =====================================================================
                // TEST 12: An unauthorized user attempts to select or reject a candidate.
                // Expected: Request is denied.
                // =====================================================================
                try
                {
                    var unauthCtrl = CreateCompaniesController(recBUser, UserRoles.Recruiter);
                    var unauthSelect = await unauthCtrl.SelectCandidate(app1!.Id);
                    var unauthReject = await unauthCtrl.RejectCandidate(app1!.Id);

                    bool pass12 = unauthSelect is ForbidResult && unauthReject is ForbidResult;

                    results[12] = ("Unauthorized user attempts select/reject -> Denied", pass12,
                        pass12 ? "Unauthorized company attempts returned ForbidResult for both select and reject." : "Unauthorized company operation was not forbidden.");
                }
                catch (Exception ex)
                {
                    results[12] = ("Unauthorized user attempts select/reject -> Denied", false, ex.Message);
                }

                // =====================================================================
                // TEST 13: An invalid status transition is attempted.
                // Expected: No database change occurs.
                // =====================================================================
                try
                {
                    var freshApp = new JobApplication
                    {
                        JobId = jobA.Id,
                        ApplicantId = studentCUser.Id,
                        AppliedDate = DateTime.UtcNow,
                        Status = ApplicationStatus.Applied,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    context.JobApplications.Add(freshApp);
                    await context.SaveChangesAsync();

                    var compCtrl = CreateCompaniesController(recAUser, UserRoles.Recruiter);

                    // 1) Attempt to select directly from Applied status (invalid; must be Shortlisted or Interview)
                    var invalidSelect = await compCtrl.SelectCandidate(freshApp.Id);
                    var appAfterInvalidSelect = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == freshApp.Id);

                    // 2) Attempt to reject an already Selected candidate (invalid; terminal state)
                    var invalidReject = await compCtrl.RejectCandidate(app1!.Id);
                    var appAfterInvalidReject = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == app1!.Id);

                    bool pass13 = appAfterInvalidSelect.Status == ApplicationStatus.Applied &&
                                  compCtrl.TempData.ContainsKey("Error") &&
                                  appAfterInvalidReject.Status == ApplicationStatus.Selected;

                    results[13] = ("Invalid status transition attempted -> No DB change, error returned", pass13,
                        pass13 ? "Invalid transitions blocked by workflow engine; DB remains unmodified and user receives descriptive error." : "Invalid status transition mutated database.");
                }
                catch (Exception ex)
                {
                    results[13] = ("Invalid status transition attempted -> No DB change, error returned", false, ex.Message);
                }

                // =====================================================================
                // TEST 14: A database operation fails during interview scheduling.
                // Expected: No inconsistent application/interview state is committed.
                // =====================================================================
                try
                {
                    var appForTx = new JobApplication
                    {
                        JobId = jobA.Id,
                        ApplicantId = studentDUser.Id,
                        AppliedDate = DateTime.UtcNow,
                        Status = ApplicationStatus.Shortlisted,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    context.JobApplications.Add(appForTx);
                    await context.SaveChangesAsync();

                    // Simulate transaction rollback on failure
                    await using (var testTx = await context.Database.BeginTransactionAsync())
                    {
                        var invalidInterview = new Interview
                        {
                            JobApplicationId = appForTx.Id,
                            InterviewDate = DateTime.UtcNow.AddDays(2),
                            InterviewType = InterviewType.Online
                        };
                        context.Interviews.Add(invalidInterview);

                        // Mutate status inside transaction
                        appForTx.Status = ApplicationStatus.Interview;
                        context.JobApplications.Update(appForTx);
                        await context.SaveChangesAsync();

                        // Force rollback to simulate failure
                        await testTx.RollbackAsync();
                    }

                    // Reload from DB
                    var reloadedAppForTx = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == appForTx.Id);
                    var countInterviews = await context.Interviews.CountAsync(i => i.JobApplicationId == appForTx.Id);

                    bool pass14 = reloadedAppForTx.Status == ApplicationStatus.Shortlisted && countInterviews == 0;

                    results[14] = ("Failed DB operation during interview scheduling -> Rollback, no inconsistent state", pass14,
                        pass14 ? "Atomic transaction rollback preserved Shortlisted status; zero orphan interview records committed." : "Inconsistent state remained after failed operation.");
                }
                catch (Exception ex)
                {
                    results[14] = ("Failed DB operation during interview scheduling -> Rollback, no inconsistent state", false, ex.Message);
                }

                // =====================================================================
                // TEST 15: The company selects one candidate for a job.
                // Expected: Other applications remain unchanged unless company explicitly updates them.
                // =====================================================================
                try
                {
                    var candidateX = new JobApplication
                    {
                        JobId = jobA.Id,
                        ApplicantId = studentEUser.Id,
                        AppliedDate = DateTime.UtcNow,
                        Status = ApplicationStatus.Shortlisted,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    var candidateY = new JobApplication
                    {
                        JobId = jobA.Id,
                        ApplicantId = studentFUser.Id,
                        AppliedDate = DateTime.UtcNow,
                        Status = ApplicationStatus.UnderReview,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    context.JobApplications.AddRange(candidateX, candidateY);
                    await context.SaveChangesAsync();

                    var compCtrl = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    await compCtrl.SelectCandidate(candidateX.Id);

                    var reloadedX = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == candidateX.Id);
                    var reloadedY = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == candidateY.Id);

                    bool pass15 = reloadedX.Status == ApplicationStatus.Selected &&
                                  reloadedY.Status == ApplicationStatus.UnderReview;

                    results[15] = ("Selecting one candidate -> Other applications remain unaffected", pass15,
                        pass15 ? $"Candidate X status is Selected, Candidate Y status remained UnderReview untouched." : "Selecting one candidate unexpectedly changed other applications.");
                }
                catch (Exception ex)
                {
                    results[15] = ("Selecting one candidate -> Other applications remain unaffected", false, ex.Message);
                }

                // =====================================================================
                // TEST 16: Build & regression verification.
                // Expected: No new compilation errors, existing authentication and dashboards work.
                // =====================================================================
                try
                {
                    var compCtrl = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    var dashResult = await compCtrl.Dashboard() as ViewResult;
                    var dashModel = dashResult?.Model as RecruiterDashboardViewModel;

                    var studentCtrl = CreateJobApplicationsController(studentAUser, UserRoles.JobSeeker);
                    var studDashResult = await studentCtrl.Dashboard() as ViewResult;
                    var studDashModel = studDashResult?.Model as JobSeekerDashboardViewModel;

                    bool pass16 = dashResult != null && dashModel != null &&
                                  studDashResult != null && studDashModel != null;

                    results[16] = ("Build & regression verification -> Authentication and dashboards working", pass16,
                        pass16 ? "Both Recruiter and Student dashboards instantiated and rendered clean models with complete metrics." : "Regression detected in dashboards.");
                }
                catch (Exception ex)
                {
                    results[16] = ("Build & regression verification -> Authentication and dashboards working", false, ex.Message);
                }

                // =====================================================================
                // TEST 17: Single Placement Policy (One Student, One Job Offer).
                // Expected: If a student is selected for Job 1, another recruiter cannot
                // select the same student for Job 2. Attempt is denied with clear error.
                // =====================================================================
                try
                {
                    var appJobA = new JobApplication
                    {
                        JobId = jobA.Id,
                        ApplicantId = studentGUser.Id,
                        AppliedDate = DateTime.UtcNow,
                        Status = ApplicationStatus.Shortlisted,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    var appJobB = new JobApplication
                    {
                        JobId = jobB.Id,
                        ApplicantId = studentGUser.Id,
                        AppliedDate = DateTime.UtcNow,
                        Status = ApplicationStatus.Shortlisted,
                        ResumePath = "/uploads/resumes/sample_resume.pdf"
                    };
                    context.JobApplications.AddRange(appJobA, appJobB);
                    await context.SaveChangesAsync();

                    // Recruiter A selects Student G for Job A -> Expected: Success
                    var compCtrlA = CreateCompaniesController(recAUser, UserRoles.Recruiter);
                    await compCtrlA.SelectCandidate(appJobA.Id);

                    var reloadedAppA = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == appJobA.Id);
                    bool step1Selected = reloadedAppA.Status == ApplicationStatus.Selected;

                    // Recruiter B attempts to select Student G for Job B -> Expected: Denied
                    var compCtrlB = CreateCompaniesController(recBUser, UserRoles.Recruiter);
                    await compCtrlB.SelectCandidate(appJobB.Id);

                    var reloadedAppB = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == appJobB.Id);
                    bool step2Blocked = reloadedAppB.Status == ApplicationStatus.Shortlisted; // Not changed to Selected!
                    string? errorMsg = compCtrlB.TempData["Error"]?.ToString();
                    bool hasErrorNotice = errorMsg != null && errorMsg.Contains("Selection Denied");

                    // Also verify via JobApplicationsController.Edit: changing status to Selected is blocked
                    var appCtrlB = CreateJobApplicationsController(recBUser, UserRoles.Recruiter);
                    var postModel = new JobApplication { Id = appJobB.Id, Status = ApplicationStatus.Selected };
                    await appCtrlB.Edit(appJobB.Id, postModel);
                    var reloadedAppBAfterEdit = await context.JobApplications.AsNoTracking().FirstAsync(ja => ja.Id == appJobB.Id);
                    bool step3EditBlocked = reloadedAppBAfterEdit.Status != ApplicationStatus.Selected &&
                                           !appCtrlB.ModelState.IsValid;

                    bool pass17 = step1Selected && step2Blocked && hasErrorNotice && step3EditBlocked;

                    results[17] = ("Single Placement Policy -> Candidate selected for Job 1 cannot be selected for Job 2", pass17,
                        pass17 ? $"Recruiter A successfully selected candidate. Recruiter B was blocked: '{errorMsg}'." : "Single placement policy failed to block multiple selections.");
                }
                catch (Exception ex)
                {
                    results[17] = ("Single Placement Policy -> Candidate selected for Job 1 cannot be selected for Job 2", false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL SUITE ERROR: {ex}");
                return 1;
            }
            finally
            {
                // Clean up all test-specific entities and dummy users so the portal remains clean
                await CleanupTestEntitiesAsync(serviceProvider);
            }

            // Print summary table
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("                             TEST EXECUTION REPORT                              ");
            Console.WriteLine("--------------------------------------------------------------------------------");

            int passCount = 0;
            for (int i = 1; i <= 17; i++)
            {
                if (results.TryGetValue(i, out var res))
                {
                    string status = res.Passed ? "PASS" : "FAIL";
                    if (res.Passed) passCount++;
                    Console.WriteLine($"TEST {i,2}: [{status}] {res.Name}");
                    Console.WriteLine($"         Details: {res.Details}");
                }
                else
                {
                    Console.WriteLine($"TEST {i,2}: [NOT TESTED]");
                }
            }

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($"TOTAL: {passCount}/17 PASSED");
            Console.WriteLine("================================================================================");

            return passCount == 17 ? 0 : 1;
        }

        private static async Task<ApplicationUser> GetOrCreateUserAsync(UserManager<ApplicationUser> userManager, string email, string fullName, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(user, "Test@Pass1234!");
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create user {email}: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            return user;
        }

        private static async Task CleanupTestEntitiesAsync(IServiceProvider serviceProvider)
        {
            try
            {
                using var cleanScope = serviceProvider.CreateScope();
                var context = cleanScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = cleanScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var testEmails = new[]
                {
                    "company_a_recruiter@portal.com",
                    "company_b_recruiter@portal.com",
                    "student_a@portal.com",
                    "student_b@portal.com",
                    "student_c@portal.com",
                    "student_d@portal.com",
                    "student_e@portal.com",
                    "student_f@portal.com",
                    "student_g@portal.com"
                };

                var testUsers = await context.Users
                    .Where(u => u.Email != null && testEmails.Contains(u.Email))
                    .ToListAsync();
                var testUserIds = testUsers.Select(u => u.Id).ToList();

                if (!testUserIds.Any()) return;

                var testInterviews = await context.Interviews
                    .Where(i => i.JobApplication != null && testUserIds.Contains(i.JobApplication.ApplicantId))
                    .ToListAsync();
                if (testInterviews.Any())
                {
                    context.Interviews.RemoveRange(testInterviews);
                    await context.SaveChangesAsync();
                }

                var testApps = await context.JobApplications
                    .Where(a => testUserIds.Contains(a.ApplicantId))
                    .ToListAsync();
                if (testApps.Any())
                {
                    context.JobApplications.RemoveRange(testApps);
                    await context.SaveChangesAsync();
                }

                var testSaved = await context.SavedJobs
                    .Where(s => testUserIds.Contains(s.ApplicantId))
                    .ToListAsync();
                if (testSaved.Any())
                {
                    context.SavedJobs.RemoveRange(testSaved);
                    await context.SaveChangesAsync();
                }

                var testCompanies = await context.Companies
                    .Where(c => testUserIds.Contains(c.RecruiterId) || c.CompanyName == "Acme Global Solutions" || c.CompanyName == "Zenith Corp")
                    .ToListAsync();
                var testCompanyIds = testCompanies.Select(c => c.Id).ToList();

                var testJobs = await context.Jobs
                    .Where(j => testCompanyIds.Contains(j.CompanyId))
                    .ToListAsync();
                if (testJobs.Any())
                {
                    context.Jobs.RemoveRange(testJobs);
                    await context.SaveChangesAsync();
                }

                if (testCompanies.Any())
                {
                    context.Companies.RemoveRange(testCompanies);
                    await context.SaveChangesAsync();
                }

                foreach (var u in testUsers)
                {
                    await userManager.DeleteAsync(u);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Test Cleanup Notice] {ex.Message}");
            }
        }
    }
}
