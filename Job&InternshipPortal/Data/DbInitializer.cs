using Job_InternshipPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration? configuration = null)
        {
            // ── Apply pending migrations ──────────────────────────────────────
            await context.Database.MigrateAsync();

            // ── Seed roles ────────────────────────────────────────────────────
            string[] roles = { UserRoles.Admin, UserRoles.Recruiter, UserRoles.JobSeeker };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Seed the initial admin account ────────────────────────────────
            var adminEmail    = !string.IsNullOrWhiteSpace(configuration?["AdminSettings:Email"]) 
                                ? configuration["AdminSettings:Email"]! 
                                : "admin@jobportal.com";
            var adminPassword = !string.IsNullOrWhiteSpace(configuration?["AdminSettings:Password"]) 
                                ? configuration["AdminSettings:Password"]! 
                                : "Admin@12345";
            var adminFullName = configuration?["AdminSettings:FullName"] ?? "System Administrator";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName       = adminEmail,
                    Email          = adminEmail,
                    FullName       = adminFullName,
                    EmailConfirmed = true    // No email service; confirm programmatically
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                }
                else
                {
                    var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
                    var logger = loggerFactory.CreateLogger(nameof(DbInitializer));
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create admin user: {Errors}", errors);
                }
            }
            else
            {
                // Admin exists — ensure role is assigned (handles re-run after role deletion)
                if (!await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            }
        }

        public static async Task ResetToCleanStateAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration? configuration = null)
        {
            // 1. Remove all application lifecycle data
            var allInterviews = await context.Interviews.ToListAsync();
            if (allInterviews.Any()) context.Interviews.RemoveRange(allInterviews);

            var allApps = await context.JobApplications.ToListAsync();
            if (allApps.Any()) context.JobApplications.RemoveRange(allApps);

            var allSaved = await context.SavedJobs.ToListAsync();
            if (allSaved.Any()) context.SavedJobs.RemoveRange(allSaved);

            var allJobs = await context.Jobs.ToListAsync();
            if (allJobs.Any()) context.Jobs.RemoveRange(allJobs);

            var allCompanies = await context.Companies.ToListAsync();
            if (allCompanies.Any()) context.Companies.RemoveRange(allCompanies);

            await context.SaveChangesAsync();

            // 2. Remove all non-admin users
            var adminEmail = !string.IsNullOrWhiteSpace(configuration?["AdminSettings:Email"])
                ? configuration["AdminSettings:Email"]!
                : "admin@jobportal.com";

            var nonAdminUsers = await context.Users
                .Where(u => u.Email != adminEmail)
                .ToListAsync();

            foreach (var user in nonAdminUsers)
            {
                await userManager.DeleteAsync(user);
            }

            // 3. Ensure roles and built-in Admin account exist (0 companies, 0 jobs, only Admin)
            await InitializeAsync(context, userManager, roleManager, configuration);
        }
    }
}
