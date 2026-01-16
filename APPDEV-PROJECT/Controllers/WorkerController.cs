using Microsoft.AspNetCore.Mvc;
using APPDEV_PROJECT.Data;
using APPDEV_PROJECT.Models;
using APPDEV_PROJECT.Models.Entities;
using APPDEV_PROJECT.Helpers;
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
                    // ===== Validate barangay is in San Juan =====
                    if (string.IsNullOrEmpty(viewModel.Barangay) || !AddressLocationHelper.IsInSanJuan(viewModel.Barangay))
                    {
                        ModelState.AddModelError("Barangay", "Please select a valid barangay in San Juan.");
                        return View(viewModel);
                    }

                    // ===== Get coordinates for the selected barangay =====
                    var (latitude, longitude) = AddressLocationHelper.GetCoordinatesForBarangay(viewModel.Barangay);

                    if (!latitude.HasValue || !longitude.HasValue)
                    {
                        ModelState.AddModelError("Barangay", "Could not determine location for the selected barangay.");
                        return View(viewModel);
                    }

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
                        Latitude = latitude,
                        Longitude = longitude,
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

        public async Task<IActionResult> JobReqPage_W()
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return RedirectToAction("LoginPage", "Account");
                }

                // ===== Get worker profile for the logged-in user =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
                
                if (worker == null)
                {
                    return RedirectToAction("InfoPage_W");
                }

                // ===== Get job requests for this worker with Client data =====
                var jobRequests = await dbContext.JobRequests
                    .Where(j => j.WorkerId == worker.WorkerId && j.Status == "Pending")
                    .Include(j => j.Client)
                    .OrderByDescending(j => j.RequestDate)
                    .ToListAsync();

                return View(jobRequests);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error loading job requests: {ex.Message}");
                return View(new List<JobRequest>());
            }
        }

        // ===== NEW: Accept job request API =====
        [HttpPost]
        [Route("api/booking/accept/{jobRequestId}")]
        public async Task<IActionResult> AcceptJobRequest(Guid jobRequestId)
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // ===== Get worker profile for the logged-in user =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
                
                if (worker == null)
                {
                    return BadRequest(new { message = "Worker profile not found" });
                }

                // ===== Get job request =====
                var jobRequest = await dbContext.JobRequests
                    .Include(j => j.Client)
                    .FirstOrDefaultAsync(j => j.JobRequestId == jobRequestId);
                
                if (jobRequest == null)
                {
                    return BadRequest(new { message = "Job request not found" });
                }

                // ===== Verify the request is for this worker =====
                if (jobRequest.WorkerId != worker.WorkerId)
                {
                    return Unauthorized(new { message = "You cannot accept a request not meant for you" });
                }

                // ===== Update status =====
                jobRequest.Status = "Accepted";
                dbContext.JobRequests.Update(jobRequest);
                await dbContext.SaveChangesAsync();

                // ===== NEW: Create Conversation for messaging =====
                var conversation = new Conversation
                {
                    ConversationId = Guid.NewGuid(),
                    ClientId = jobRequest.ClientId,
                    WorkerId = jobRequest.WorkerId,
                    JobRequestId = jobRequest.JobRequestId,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                dbContext.Conversations.Add(conversation);
                await dbContext.SaveChangesAsync();

                // ===== NEW: Create notification for client =====
                var clientNotification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    RecipientId = jobRequest.Client.UserId,
                    JobRequestId = jobRequest.JobRequestId,
                    SenderId = userId,
                    Title = "Booking Accepted",
                    Message = $"{worker.FullName} accepted your booking request",
                    Type = "booking",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                dbContext.Notifications.Add(clientNotification);
                await dbContext.SaveChangesAsync();

                return Ok(new { message = "Job request accepted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error accepting request: {ex.Message}" });
            }
        }

        // ===== NEW: Reject job request API =====
        [HttpPost]
        [Route("api/booking/reject/{jobRequestId}")]
        public async Task<IActionResult> RejectJobRequest(Guid jobRequestId)
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // ===== Get worker profile for the logged-in user =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
                
                if (worker == null)
                {
                    return BadRequest(new { message = "Worker profile not found" });
                }

                // ===== Get job request =====
                var jobRequest = await dbContext.JobRequests
                    .Include(j => j.Client)
                    .FirstOrDefaultAsync(j => j.JobRequestId == jobRequestId);
                
                if (jobRequest == null)
                {
                    return BadRequest(new { message = "Job request not found" });
                }

                // ===== Verify the request is for this worker =====
                if (jobRequest.WorkerId != worker.WorkerId)
                {
                    return Unauthorized(new { message = "You cannot reject a request not meant for you" });
                }

                // ===== Update status =====
                jobRequest.Status = "Rejected";
                dbContext.JobRequests.Update(jobRequest);
                await dbContext.SaveChangesAsync();

                // ===== NEW: Create notification for client =====
                var clientNotification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    RecipientId = jobRequest.Client.UserId,
                    JobRequestId = jobRequest.JobRequestId,
                    SenderId = userId,
                    Title = "Booking Rejected",
                    Message = $"{worker.FullName} rejected your booking request",
                    Type = "booking",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                dbContext.Notifications.Add(clientNotification);
                await dbContext.SaveChangesAsync();

                return Ok(new { message = "Job request rejected" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error rejecting request: {ex.Message}" });
            }
        }

        public async Task<IActionResult> NotifPage_W()
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return RedirectToAction("LoginPage", "Account");
                }

                // ===== Get worker profile for the logged-in user =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
                
                if (worker == null)
                {
                    return RedirectToAction("InfoPage_W");
                }

                // ===== Get notifications for this worker =====
                var notifications = await dbContext.Notifications
                    .Where(n => n.RecipientId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                return View(notifications);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error loading notifications: {ex.Message}");
                return View(new List<Notification>());
            }
        }

        public IActionResult ChatPage_W()
        {
            // ===== Get the logged-in user's ID from claims =====
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return RedirectToAction("LoginPage", "Account");
            }

            // ===== Get worker profile for the logged-in user =====
            var worker = dbContext.Workers.FirstOrDefault(w => w.UserId == userId);
            
            if (worker == null)
            {
                return RedirectToAction("InfoPage_W");
            }

            // ===== Get conversations for this worker =====
            var conversations = dbContext.Conversations
                .Where(c => c.WorkerId == worker.WorkerId)
                .Include(c => c.Client)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            ViewBag.Conversations = conversations;

            return View();
        }

        // ===== NEW: View worker profile as Client (Read-Only) =====
        // This action displays a worker's profile in read-only mode for clients
        [HttpGet]
        public async Task<IActionResult> ViewWorkerProfile(Guid workerId)
        {
            try
            {
                // ===== Get worker profile by ID =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.WorkerId == workerId);

                if (worker == null)
                {
                    return NotFound();
                }

                // Pass to a read-only view
                return View("ViewWorkerProfile", worker);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error loading worker profile: {ex.Message}");
                return RedirectToAction("SearchPage_C", "Client");
            }
        }

        // ===== NEW: Get messages for a conversation =====
        [HttpGet]
        [Route("api/worker/messages/{conversationId}")]
        public async Task<IActionResult> GetMessages(Guid conversationId)
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // ===== Get worker profile =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.UserId == userId);
                
                if (worker == null)
                {
                    return BadRequest(new { message = "Worker profile not found" });
                }

                // ===== Verify conversation belongs to worker and include Client =====
                var conversation = await dbContext.Conversations
                    .Include(c => c.Client)
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.WorkerId == worker.WorkerId);
                
                if (conversation == null)
                {
                    return BadRequest(new { message = "Conversation not found" });
                }

                var messages = await dbContext.Messages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                // ===== Transform to DTO with camelCase properties =====
                var result = messages.Select(m => new
                {
                    messageId = m.MessageId,
                    conversationId = m.ConversationId,
                    senderId = m.SenderId,
                    senderName = m.SenderId == userId ? "You" : conversation.Client?.FullName ?? "Unknown",
                    content = m.Content,
                    timestamp = m.SentAt,
                    isFromWorker = m.SenderId == userId,
                    isRead = m.IsRead
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ===== NEW: Send a message =====
        [HttpPost]
        [Route("api/worker/messages/send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // ===== Verify conversation exists =====
                var conversation = await dbContext.Conversations.FindAsync(Guid.Parse(dto.ConversationId));
                
                if (conversation == null)
                {
                    return BadRequest(new { message = "Conversation not found" });
                }

                // ===== Create message =====
                var message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    ConversationId = Guid.Parse(dto.ConversationId),
                    SenderId = userId,
                    Content = dto.Content,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                dbContext.Messages.Add(message);
                
                // ===== Update conversation's last message time =====
                conversation.LastMessageAt = DateTime.Now;
                dbContext.Conversations.Update(conversation);
                
                await dbContext.SaveChangesAsync();

                return Ok(new { message = "Message sent successfully", messageId = message.MessageId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error sending message: {ex.Message}" });
            }
        }

        // ===== DTO for sending messages =====
        public class SendMessageDto
        {
            public string ConversationId { get; set; }
            public string Content { get; set; }
        }
    }
}