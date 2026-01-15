using Microsoft.AspNetCore.Mvc;
using APPDEV_PROJECT.Data;
using APPDEV_PROJECT.Models;
using APPDEV_PROJECT.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace APPDEV_PROJECT.Controllers
{
    public class WorkerController : Controller
    {
        // ===== NEW: Added database context for worker profile operations =====
        private readonly HanapBuhayDBContext dbContext;

        public WorkerController(HanapBuhayDBContext context)
        {
            dbContext = context;
        }

        public IActionResult DashboardPage_W()
        {
            return View();
        }

        // ===== INFO PAGE - GET REQUEST =====
        // Display the worker information form
        [HttpGet]
        public IActionResult InfoPage_W()
        {
            return View();
        }

        // ===== INFO PAGE - POST REQUEST =====
        // Handle worker profile creation and auto-login
        [HttpPost]
        public async Task<IActionResult> InfoPage_W(InfoPage_Worker_ViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ===== STEP 1: Get the UserId from session (set during registration) =====
                    // This connects the worker profile to the user account
                    var userIdString = HttpContext.Session.GetString("NewUserId");
                    
                    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                    {
                        // ===== If no session UserId, try to get from authenticated user =====
                        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out userId))
                        {
                            ModelState.AddModelError("", "User session expired. Please register again.");
                            return View(viewModel);
                        }
                    }

                    // ===== STEP 2: Create a new Worker entity from the viewmodel =====
                    var worker = new Worker
                    {
                        WorkerId = Guid.NewGuid(),
                        // ===== Link to User account using UserId =====
                        UserId = userId,
                        FName = viewModel.FName,
                        Mname = viewModel.Mname,
                        LName = viewModel.LName,
                        Email = viewModel.Email,
                        DateOfBirth = viewModel.DateOfBirth,
                        Sex = viewModel.Sex,
                        PhoneNumber = viewModel.PhoneNumber,
                        Address = viewModel.Address,
                        Skill = viewModel.Skill,
                        YearsOfExperience = viewModel.YearsOfExperience,
                        Accomplishments = viewModel.Accomplishments
                    };

                    // ===== STEP 3: Save worker profile to database =====
                    dbContext.Workers.Add(worker);
                    await dbContext.SaveChangesAsync();

                    // ===== STEP 4: Auto-login the user after profile creation =====
                    // Get the user from database to create authentication claims
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    
                    if (user != null)
                    {
                        // Create authentication claims for the user
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

                        // Sign in the user automatically
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties
                        );
                    }

                    // ===== STEP 5: Clear the session after profile is created =====
                    HttpContext.Session.Remove("NewUserId");
                    HttpContext.Session.Remove("UserType");

                    // Redirect to login page
                    return RedirectToAction("LoginPage", "Account");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving worker information: {ex.Message}");
                }
            }

            // If there are errors, return to the form with error messages
            return View(viewModel);
        }

        // ===== PROFILE - GET REQUEST =====
        // Display the worker's profile with their information
        [HttpGet]
        public async Task<IActionResult> Profile_W()
        {
            try
            {
                // ===== STEP 1: Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    // ===== User not authenticated, redirect to login =====
                    return RedirectToAction("LoginPage", "Account");
                }

                // ===== STEP 2: Get worker profile for the logged-in user =====
                var worker = await dbContext.Workers
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                if (worker == null)
                {
                    return RedirectToAction("InfoPage_W");
                }

                return View(worker);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error loading profile: {ex.Message}");
                return RedirectToAction("InfoPage_W");
            }
        }

        // ===== UPDATE PROFILE - POST REQUEST =====
        // Handle worker profile updates
        [HttpPost]
        public async Task<IActionResult> UpdateProfile_W(Worker model)
        {
            if (!ModelState.IsValid)
            {
                // ===== Fetch profile using authenticated user =====
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
                    return View("Profile_W", worker);
                }
                return RedirectToAction("LoginPage", "Account");
            }

            try
            {
                var workerToUpdate = await dbContext.Workers.FirstOrDefaultAsync(w => w.WorkerId == model.WorkerId);
                
                if (workerToUpdate == null)
                {
                    return RedirectToAction("InfoPage_W");
                }

                // ===== Update all profile fields =====
                workerToUpdate.FName = model.FName;
                workerToUpdate.Mname = model.Mname;
                workerToUpdate.LName = model.LName;
                workerToUpdate.Email = model.Email;
                workerToUpdate.PhoneNumber = model.PhoneNumber;
                workerToUpdate.DateOfBirth = model.DateOfBirth;
                workerToUpdate.Sex = model.Sex;
                workerToUpdate.Address = model.Address;
                workerToUpdate.Skill = model.Skill;
                workerToUpdate.YearsOfExperience = model.YearsOfExperience;
                workerToUpdate.Accomplishments = model.Accomplishments;

                dbContext.Workers.Update(workerToUpdate);
                await dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile_W");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating profile: {ex.Message}");
                // ===== Fetch profile using authenticated user =====
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
                    return View("Profile_W", worker);
                }
                return RedirectToAction("LoginPage", "Account");
            }
        }

        // ===== UPDATE ABOUT ME - POST REQUEST =====
        // Handle updating only the About Me (Accomplishments) section
        [HttpPost]
        public async Task<IActionResult> UpdateAboutMe_W(Guid workerId, string aboutMe)
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return RedirectToAction("LoginPage", "Account");
                }

                // ===== Get worker profile =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.WorkerId == workerId && w.UserId == userId);

                if (worker == null)
                {
                    return RedirectToAction("Profile_W");
                }

                // ===== Update only the Accomplishments field =====
                worker.Accomplishments = aboutMe;
                dbContext.Workers.Update(worker);
                await dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = "About Me updated successfully!";
                return RedirectToAction("Profile_W");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating About Me: {ex.Message}");
                return RedirectToAction("Profile_W");
            }
        }

        public IActionResult JobReqPage_W()
        {
            return View();
        }

        public IActionResult NotifPage_W()
        {
            return View();
        }

        public IActionResult ChatPage_W()
        {
            return View();
        }
    }
}