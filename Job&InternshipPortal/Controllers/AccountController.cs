using Job_InternshipPortal.Models;
using Job_InternshipPortal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Job_InternshipPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

<<<<<<< HEAD
            // ── Server-side role allowlist: ONLY JobSeeker and Recruiter are
            //    permitted through public registration. Any other value
            //    (including "Admin") is rejected outright here — the ternary
            //    in the original code was the only guard; this makes it explicit.
            if (model.Role != UserRoles.JobSeeker && model.Role != UserRoles.Recruiter)
            {
                ModelState.AddModelError("Role", "Invalid account type selected.");
            }

=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
<<<<<<< HEAD
                UserName  = model.Email,
                Email     = model.Email,
                FullName  = model.FullName
=======
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
<<<<<<< HEAD
                // Allowlisted role — Admin can never reach here
                var roleName = model.Role == UserRoles.Recruiter ? UserRoles.Recruiter : UserRoles.JobSeeker;

=======
                // Ensure role exists
                var roleName = model.Role == UserRoles.Recruiter ? UserRoles.Recruiter : UserRoles.JobSeeker;
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }

                await _userManager.AddToRoleAsync(user, roleName);

<<<<<<< HEAD
                _logger.LogInformation("User {Email} registered with role {Role}.", user.Email, roleName);
=======
                _logger.LogInformation("User created a new account with password. Role: {Role}", roleName);
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169

                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = $"Welcome to Job & Internship Portal, {user.FullName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (roleName == UserRoles.Recruiter)
                {
<<<<<<< HEAD
                    return RedirectToAction("Dashboard", "Companies");
                }

                return RedirectToAction("Dashboard", "JobApplications");
=======
                    return RedirectToAction("Index", "Companies");
                }

                return RedirectToAction("Index", "Jobs");
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? model.ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

<<<<<<< HEAD
            // Look up user first so we can return a generic error if not found,
            // without exposing whether the email exists (timing-safe: same message in both branches)
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Generic message — do not reveal that the email is unregistered
=======
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
                ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
                return View(model);
            }

<<<<<<< HEAD
            // lockoutOnFailure: true — respects the lockout policy configured in Program.cs
=======
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
<<<<<<< HEAD
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in.", user.Email);
                TempData["Success"] = $"Welcome back, {user.FullName}!";

                // Validate ReturnUrl to prevent open-redirect attacks
=======
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                TempData["Success"] = $"Welcome back, {user.FullName}!";

>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
                var targetUrl = returnUrl ?? model.ReturnUrl;
                if (!string.IsNullOrEmpty(targetUrl) && Url.IsLocalUrl(targetUrl))
                {
                    return Redirect(targetUrl);
                }

<<<<<<< HEAD
                if (await _userManager.IsInRoleAsync(user, UserRoles.Admin))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (await _userManager.IsInRoleAsync(user, UserRoles.Recruiter))
                {
                    return RedirectToAction("Dashboard", "Companies");
                }

                return RedirectToAction("Dashboard", "JobApplications");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out.", user.Email);
                ModelState.AddModelError(string.Empty,
                    "Your account has been locked out after too many failed attempts. Please try again in 15 minutes.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
=======
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your credentials.");
>>>>>>> b82863a76e709b1c678db16168ef0cd6843c8169
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
