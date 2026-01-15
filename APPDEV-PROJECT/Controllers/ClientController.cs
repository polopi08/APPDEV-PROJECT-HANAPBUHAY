using APPDEV_PROJECT.Data;
using APPDEV_PROJECT.Helpers;
using APPDEV_PROJECT.Models;
using APPDEV_PROJECT.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;


namespace APPDEV_PROJECT.Controllers
{
    public class ClientController : Controller
    {
        private readonly HanapBuhayDBContext dbContext;

        public ClientController(HanapBuhayDBContext context)
        {
            dbContext = context;
        }

        [HttpGet]
        [Route("api/workers/nearby")]
        public async Task<IActionResult> GetNearbyWorkers(double lat, double lng, string skill = "", string search = "")
        {
            try
            {
                // ===== NEW: Fetch actual workers from database =====
                var workers = await dbContext.Workers.ToListAsync();

                // ===== Convert to WorkerWithDistance objects with actual GUIDs =====
                var workersWithDistance = workers.Select(w => new
                {
                    Id = w.WorkerId.ToString(), // Convert GUID to string
                    Name = w.FullName,
                    Skill = w.Skill,
                    Lat = 14.604432, // Default to San Juan center if not stored
                    Lng = 121.029950,
                    DistanceKm = 0.0 // Will be calculated
                }).ToList();

                // ===== Calculate distances and filter =====
                var filteredWorkers = workersWithDistance
                    .Select(w => new
                    {
                        w.Id,
                        w.Name,
                        w.Skill,
                        w.Lat,
                        w.Lng,
                        DistanceKm = WorkerFilterHelper.CalculateDistance(lat, lng, w.Lat, w.Lng)
                    })
                    .Where(w => w.DistanceKm <= WorkerFilterHelper.MaxDistanceKm)
                    .Where(w => string.IsNullOrEmpty(skill) || 
                               w.Skill.Equals(skill, StringComparison.OrdinalIgnoreCase))
                    .Where(w => string.IsNullOrEmpty(search) || 
                               w.Skill.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               w.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(w => w.DistanceKm)
                    .ToList();

                return Json(filteredWorkers);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /* changes, this is for messaging*/
                [HttpGet]
                [Route("api/messages/conversations")]
                public IActionResult GetConversations(string search = "")
                {
                    try
                    {
                        var conversations = string.IsNullOrEmpty(search) 
                            ? MessageHelper.GetConversations()
                            : MessageHelper.SearchConversations(search);
                        return Json(conversations);
                    }
                    catch (System.Exception ex)
                    {
                        return BadRequest(new { error = ex.Message });
                    }
                }

                [HttpGet]
                [Route("api/messages/{conversationId}")]
                public IActionResult GetMessages(int conversationId)
                {
                    try
                    {
                        var messages = MessageHelper.GetMessages(conversationId);
                        return Json(messages);
                    }
                    catch (System.Exception ex)
                    {
                        return BadRequest(new { error = ex.Message });
                    }
                }

                [HttpPost]
                [Route("api/messages/send")]
                public IActionResult SendMessage([FromBody] SendMessageRequest request)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(request.Content))
                            return BadRequest(new { error = "Message content cannot be empty" });

                        var message = MessageHelper.SendMessage(request.ConversationId, request.Content);
                        return Json(message);
                    }
                    catch (System.Exception ex)
                    {
                        return BadRequest(new { error = ex.Message });
                    }
                }

                public class SendMessageRequest
                {
                    public int ConversationId { get; set; }
                    public string Content { get; set; }
                }

        public IActionResult SearchPage_C()
        {
            return View();
        }
// nandito sophia
        [HttpGet]
        public IActionResult InfoPage_C()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> InfoPage_C(InfoPage_Client_ViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ===== NEW: Get the UserId from session (set during registration) =====
                    // This connects the client profile to the user account
                    var userIdString = HttpContext.Session.GetString("NewUserId");
                    
                    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                    {
                        // ===== If no session UserId, try to get from authenticated user =====
                        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out userId))
                        {
                            ModelState.AddModelError("", "User session expired. Please register again.");
                            return View(viewModel);
                        }
                    }

                    // Create a new Client entity from the viewmodel
                    var client = new Client
                    {
                        ClientId = Guid.NewGuid(),
                        // ===== NEW: Link to User account using UserId =====
                        UserId = userId,
                        FName = viewModel.FName,
                        Mname = viewModel.Mname,
                        LName = viewModel.LName,
                        Email = viewModel.Email,
                        DateOfBirth = viewModel.DateOfBirth,
                        Sex = viewModel.Sex,
                        PhoneNumber = viewModel.PhoneNumber,
                        Address = viewModel.Address
                    };

                    // Add to database
                    dbContext.Clients.Add(client);
                    await dbContext.SaveChangesAsync();

                    // ===== NEW: Auto-login the user after profile creation =====
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

                    // ===== NEW: Clear the session after profile is created =====
                    HttpContext.Session.Remove("NewUserId");
                    HttpContext.Session.Remove("UserType");

                    // Redirect to login page as requested
                    return RedirectToAction("LoginPage", "Account");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving client information: {ex.Message}");
                }
            }

            // If there are errors, return to the form with error messages
            return View(viewModel);
        }

        // ===== UPDATED: Profile GET now uses authenticated user instead of latest client =====
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                // ===== NEW: Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    // ===== User not authenticated, redirect to login =====
                    return RedirectToAction("LoginPage", "Account");
                }

                // ===== NEW: Get client profile for the logged-in user =====
                var client = await dbContext.Clients
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (client == null)
                {
                    return RedirectToAction("InfoPage_C");
                }

                return View("ProfilePage_C", client);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error loading profile: {ex.Message}");
                return RedirectToAction("InfoPage_C");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(Client model)
        {
            if (!ModelState.IsValid)
            {
                // ===== NEW: Fetch profile using authenticated user instead of latest =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                    return View("ProfilePage_C", client);
                }
                return RedirectToAction("LoginPage", "Account");
            }

            try
            {
                var clientToUpdate = await dbContext.Clients.FirstOrDefaultAsync(c => c.ClientId == model.ClientId);
                
                if (clientToUpdate == null)
                {
                    return RedirectToAction("InfoPage_C");
                }

                clientToUpdate.FName = model.FName;
                clientToUpdate.Mname = model.Mname;
                clientToUpdate.LName = model.LName;
                clientToUpdate.Email = model.Email;
                clientToUpdate.PhoneNumber = model.PhoneNumber;
                clientToUpdate.DateOfBirth = model.DateOfBirth;
                clientToUpdate.Sex = model.Sex;
                clientToUpdate.Address = model.Address;

                dbContext.Clients.Update(clientToUpdate);
                await dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating profile: {ex.Message}");
                // ===== NEW: Fetch profile using authenticated user =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                    return View("ProfilePage_C", client);
                }
                return RedirectToAction("LoginPage", "Account");
            }
        }

        public IActionResult ChatPage()
        {
            return View();
        }

        public IActionResult Messages()
        {
            return View("ChatPage");
        }

        public IActionResult Notifications()
        {
            return View("NotifPage");
        }
    }
}