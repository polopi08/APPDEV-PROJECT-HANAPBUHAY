using Microsoft.AspNetCore.Mvc;
using APPDEV_PROJECT.Data;
using APPDEV_PROJECT.Helpers;
using APPDEV_PROJECT.Models;
using APPDEV_PROJECT.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace APPDEV_PROJECT.Controllers
{
    public class AccountController : Controller
    {
        // ===== NEW: Added database context and logger for authentication operations =====
        private readonly HanapBuhayDBContext dbContext;
        private readonly ILogger<AccountController> logger;

        public AccountController(HanapBuhayDBContext context, ILogger<AccountController> logger)
        {
            dbContext = context;
            this.logger = logger;
        }

        // ===== LOGIN PAGE - GET REQUEST =====
        // Display the login form
        public IActionResult LoginPage()
        {
            return View();
        }

        // ===== LOGIN - POST REQUEST =====
        // Handle user login - verify email and password
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View("LoginPage", model);
            }

            try
            {
                // ===== STEP 1: Find user by email and user type =====
                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.UserType == model.UserType);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View("LoginPage", model);
                }

                // ===== STEP 2: Verify password =====
                // Use AuthenticationHelper to check if provided password matches the hash
                if (!AuthenticationHelper.VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View("LoginPage", model);
                }

                // ===== STEP 3: Update last login time =====
                user.LastLoginAt = DateTime.UtcNow;
                dbContext.Users.Update(user);
                await dbContext.SaveChangesAsync();

                // ===== STEP 4: Create authentication claims =====
                // Claims are used to identify the logged-in user throughout the application
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserType", user.UserType),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                // ===== STEP 5: Sign in the user =====
                // This creates an authentication cookie that identifies the user
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                // ===== STEP 6: Redirect to appropriate page based on user type =====
                if (model.UserType == "Client")
                {
                    return RedirectToAction("Profile", "Client");
                }
                else if (model.UserType == "Worker")
                {
                    return RedirectToAction("Profile_W", "Worker");
                }

                return RedirectToAction("LandingPage", "Home");
            }
            catch (Exception ex)
            {
                logger.LogError($"Login error: {ex.Message}");
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View("LoginPage", model);
            }
        }

        // ===== REGISTER PAGE - GET REQUEST =====
        // Display the registration form
        public IActionResult RegisterPage()
        {
            return View();
        }

        // ===== REGISTER - POST REQUEST =====
        // Handle user registration - create new User account
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please fill all fields correctly.");
                return View("RegisterPage", model);
            }

            // ===== VALIDATION 1: Check if passwords match =====
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View("RegisterPage", model);
            }

            try
            {
                // ===== VALIDATION 2: Check if email already exists =====
                var existingUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Email is already registered.");
                    return View("RegisterPage", model);
                }

                // ===== STEP 1: Create new User account =====
                var newUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = model.Email,
                    UserType = model.UserType,
                    // ===== Password is hashed using AuthenticationHelper =====
                    // Never store plain text passwords in database
                    PasswordHash = AuthenticationHelper.HashPassword(model.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // ===== STEP 2: Save new user to database =====
                dbContext.Users.Add(newUser);
                await dbContext.SaveChangesAsync();

                // ===== STEP 3: Redirect to info page to create profile =====
                // Store the UserId in session so InfoPage can link the profile to this user
                HttpContext.Session.SetString("NewUserId", newUser.UserId.ToString());
                HttpContext.Session.SetString("UserType", model.UserType);

                if (model.UserType == "Client")
                {
                    return RedirectToAction("InfoPage_C", "Client");
                }
                else if (model.UserType == "Worker")
                {
                    return RedirectToAction("InfoPage_W", "Worker");
                }

                return View("RegisterPage", model);
            }
            catch (Exception ex)
            {
                logger.LogError($"Registration error: {ex.Message}");
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View("RegisterPage", model);
            }
        }

        // ===== LOGOUT =====
        // Sign out the user and clear authentication
        public async Task<IActionResult> Logout()
        {
            // ===== Sign out removes the authentication cookie =====
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LandingPage", "Home");
        }
    }
}