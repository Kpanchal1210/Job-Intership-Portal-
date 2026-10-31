using Job_InternshipPortal.Data;
using Job_InternshipPortal.Models;
<<<<<<< HEAD
using Job_InternshipPortal.Services;
=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
<<<<<<< HEAD
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // ── Account confirmation ──────────────────────────────────────────────
    options.SignIn.RequireConfirmedAccount = false;   // No email service configured

    // ── Password policy: require at least one digit, one uppercase,
    //    one lowercase, one non-alphanumeric, minimum 8 characters.
    //    Previous policy (all false, length 6) was too weak.
    options.Password.RequireDigit           = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength         = 8;
    options.Password.RequiredUniqueChars    = 4;

    // ── Lockout: block account after 5 failed attempts for 15 minutes ────
    options.Lockout.AllowedForNewUsers      = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);

    // ── User settings ────────────────────────────────────────────────────
    options.User.RequireUniqueEmail = true;
=======
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

<<<<<<< HEAD
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath       = "/Account/Login";
    options.LogoutPath      = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan  = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    // ── Cookie security hardening ─────────────────────────────────────────
    // HttpOnly: prevents JavaScript from reading the cookie (XSS mitigation)
    options.Cookie.HttpOnly = true;
    // SameSite=Strict: prevents the cookie from being sent on cross-site requests (CSRF mitigation)
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    // SecurePolicy=Always: cookie is only sent over HTTPS
    // Use SameAsRequest in development so localhost HTTP still works
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest
        : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.Name = ".JobPortal.Auth";
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IApplicationWorkflowService, ApplicationWorkflowService>();
=======
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
<<<<<<< HEAD
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
=======
}

app.UseHttpsRedirection();
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

<<<<<<< HEAD
// Initialize database and seed initial demo data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        await DbInitializer.InitializeAsync(context, userManager, roleManager, configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (args.Length > 0 && args.Contains("--reset-db"))
{
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var uMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var rMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await DbInitializer.ResetToCleanStateAsync(ctx, uMgr, rMgr, cfg);
    Console.WriteLine("================================================================================");
    Console.WriteLine("DATABASE RESET COMPLETED: All raw data deleted. ONLY Admin account exists.");
    Console.WriteLine($"Built-in Admin Email:    {cfg["AdminSettings:Email"] ?? "admin@jobportal.com"}");
    Console.WriteLine($"Built-in Admin Password: {cfg["AdminSettings:Password"] ?? "Admin@12345"}");
    Console.WriteLine("================================================================================");
    Environment.Exit(0);
}

if (args.Length > 0 && args.Contains("--run-tests"))
{
    var exitCode = await Job_InternshipPortal.Testing.PortalTestRunner.RunAllTestsAsync(app.Services);
    Environment.Exit(exitCode);
}
=======
app.MapRazorPages();
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169

app.Run();