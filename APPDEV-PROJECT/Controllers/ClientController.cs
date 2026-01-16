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
                // ===== Fetch actual workers from database =====
                var workers = await dbContext.Workers.ToListAsync();

                // ===== Convert to objects with worker coordinates =====
                var workersWithDistance = workers.Select(w => new
                {
                    Id = w.WorkerId.ToString(),
                    Name = w.FullName,
                    Skill = w.Skill,
                    // Use worker's actual coordinates, fallback to default if not set
                    Lat = w.Latitude ?? 14.604432,
                    Lng = w.Longitude ?? 121.029950,
                    DistanceKm = 0.0
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

        // ===== NEW: API endpoint for booking requests =====
        [HttpPost]
        [Route("api/booking/request")]
        public async Task<IActionResult> CreateBookingRequest([FromBody] CreateBookingRequestDto dto)
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // ===== Get client profile for the logged-in user =====
                var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (client == null)
                {
                    return BadRequest(new { message = "Client profile not found" });
                }

                // ===== Parse worker ID =====
                if (!Guid.TryParse(dto.WorkerId, out var workerId))
                {
                    return BadRequest(new { message = "Invalid worker ID" });
                }

                // ===== Verify worker exists =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.WorkerId == workerId);
                if (worker == null)
                {
                    return BadRequest(new { message = "Worker not found" });
                }

                // ===== Create job request =====
                var jobRequest = new JobRequest
                {
                    JobRequestId = Guid.NewGuid(),
                    ClientId = client.ClientId,
                    WorkerId = workerId,
                    ServiceDetails = dto.ServiceDetails,
                    RequestDate = DateTime.Now,
                    Status = "Pending"
                };

                dbContext.JobRequests.Add(jobRequest);
                await dbContext.SaveChangesAsync();

                // ===== NEW: Create notification for worker =====
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    RecipientId = worker.UserId,
                    JobRequestId = jobRequest.JobRequestId,
                    SenderId = userId,
                    Title = "New Booking Request",
                    Message = $"{client.FullName} sent you a booking request for {jobRequest.ServiceDetails}",
                    Type = "booking",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                dbContext.Notifications.Add(notification);
                await dbContext.SaveChangesAsync();

                return Ok(new { message = "Booking request sent successfully", jobRequestId = jobRequest.JobRequestId });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = $"Error creating booking request: {ex.Message}" });
            }
        }

        // ===== DTO for booking request =====
        public class CreateBookingRequestDto
        {
            public string WorkerId { get; set; }
            public string ServiceDetails { get; set; }
        }

        /* ===== MESSAGING ENDPOINTS - DATABASE DRIVEN ===== */
        [HttpGet]
        [Route("api/messages/conversations")]
        public async Task<IActionResult> GetConversations(string search = "")
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // ===== Get client profile for the logged-in user =====
                var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (client == null)
                {
                    return BadRequest(new { message = "Client profile not found" });
                }

                // ===== Get conversations for this client =====
                var query = dbContext.Conversations
                    .Where(c => c.ClientId == client.ClientId)
                    .Include(c => c.Worker)
                    .Include(c => c.Messages)
                    .AsQueryable();

                // ===== Apply search filter if provided =====
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => c.Worker.FullName.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                // ===== Order by last message time =====
                var conversations = await query
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .ToListAsync();

                // ===== Transform to DTO with camelCase properties =====
                var result = conversations.Select(c => new
                {
                    conversationId = c.ConversationId,
                    workerId = c.WorkerId,
                    workerName = c.Worker?.FullName ?? "Unknown Worker",
                    lastMessage = c.Messages?.OrderByDescending(m => m.SentAt).FirstOrDefault()?.Content ?? "No messages yet",
                    lastMessageTime = c.LastMessageAt ?? c.CreatedAt,
                    isOnline = true
                }).ToList();

                return Json(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/client/messages/{conversationId}")]
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

                // ===== Get client profile for the logged-in user =====
                var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (client == null)
                {
                    return BadRequest(new { message = "Client profile not found" });
                }

                // ===== Verify conversation belongs to client and include Worker =====
                var conversation = await dbContext.Conversations
                    .Include(c => c.Worker)
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.ClientId == client.ClientId);
                
                if (conversation == null)
                {
                    return BadRequest(new { message = "Conversation not found" });
                }

                // ===== Get messages for conversation =====
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
                    senderName = m.SenderId == userId ? "You" : conversation.Worker?.FullName ?? "Unknown",
                    content = m.Content,
                    timestamp = m.SentAt,
                    isFromClient = m.SenderId == userId,
                    isRead = m.IsRead
                }).ToList();

                return Json(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost]
        [Route("api/client/messages/send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                // ===== Validate input =====
                if (request == null || string.IsNullOrEmpty(request.Content))
                {
                    return BadRequest(new { error = "Message content cannot be empty" });
                }

                if (string.IsNullOrEmpty(request.ConversationId.ToString()))
                {
                    return BadRequest(new { error = "Conversation ID is required" });
                }

                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // ===== Get client profile for the logged-in user =====
                var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (client == null)
                {
                    return BadRequest(new { message = "Client profile not found" });
                }

                // ===== Parse ConversationId =====
                if (!Guid.TryParse(request.ConversationId.ToString(), out var conversationId))
                {
                    return BadRequest(new { error = "Invalid conversation ID format" });
                }

                // ===== Verify conversation belongs to client =====
                var conversation = await dbContext.Conversations.FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.ClientId == client.ClientId);
                
                if (conversation == null)
                {
                    return BadRequest(new { message = "Conversation not found. Please create a booking request first." });
                }

                // ===== Create message =====
                var message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    ConversationId = conversationId,
                    SenderId = userId,
                    Content = request.Content,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                dbContext.Messages.Add(message);
                
                // ===== Update conversation's last message time =====
                conversation.LastMessageAt = DateTime.Now;
                dbContext.Conversations.Update(conversation);
                
                await dbContext.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Message sent successfully",
                    messageId = message.MessageId,
                    content = message.Content,
                    timestamp = message.SentAt
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        public class SendMessageRequest
        {
            public string ConversationId { get; set; }
            public string Content { get; set; }
        }

        // ===== NEW: Get job status for a conversation =====
        [HttpGet]
        [Route("api/client/job-status/{conversationId}")]
        public async Task<IActionResult> GetJobStatus(Guid conversationId)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (client == null)
                {
                    return BadRequest(new { message = "Client profile not found" });
                }

                var conversation = await dbContext.Conversations
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.ClientId == client.ClientId);
                
                if (conversation == null)
                {
                    return BadRequest(new { message = "Conversation not found" });
                }

                var jobRequest = await dbContext.JobRequests
                    .FirstOrDefaultAsync(j => j.JobRequestId == conversation.JobRequestId);
                
                if (jobRequest == null)
                {
                    return BadRequest(new { message = "Job request not found" });
                }

                // Check if review already exists
                var existingReview = await dbContext.Reviews
                    .FirstOrDefaultAsync(r => r.JobRequestId == jobRequest.JobRequestId);

                return Ok(new 
                { 
                    jobRequestId = jobRequest.JobRequestId,
                    status = jobRequest.Status,
                    hasReview = existingReview != null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ===== NEW: Submit review for completed job =====
        [HttpPost]
        [Route("api/client/submit-review")]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (client == null)
                {
                    return BadRequest(new { message = "Client profile not found" });
                }

                var jobRequest = await dbContext.JobRequests
                    .FirstOrDefaultAsync(j => j.JobRequestId == dto.JobRequestId && j.ClientId == client.ClientId);
                
                if (jobRequest == null)
                {
                    return BadRequest(new { message = "Job request not found or not authorized" });
                }

                // Check if review already exists
                var existingReview = await dbContext.Reviews
                    .FirstOrDefaultAsync(r => r.JobRequestId == dto.JobRequestId);
                
                if (existingReview != null)
                {
                    return BadRequest(new { message = "Review already submitted for this job" });
                }

                // ===== Validate rating =====
                if (dto.Rating < 1 || dto.Rating > 5)
                {
                    return BadRequest(new { message = "Rating must be between 1 and 5" });
                }

                // ===== Create review =====
                var review = new Review
                {
                    ReviewId = Guid.NewGuid(),
                    JobRequestId = dto.JobRequestId,
                    WorkerId = jobRequest.WorkerId,
                    ClientId = client.ClientId,
                    Rating = dto.Rating,
                    ReviewText = dto.ReviewText ?? "",
                    CreatedAt = DateTime.Now
                };

                dbContext.Reviews.Add(review);

                // ===== Update job status to Reviewed =====
                jobRequest.Status = "Reviewed";
                dbContext.JobRequests.Update(jobRequest);

                // ===== Update worker's average rating =====
                var worker = await dbContext.Workers.FirstOrDefaultAsync(w => w.WorkerId == jobRequest.WorkerId);
                
                if (worker != null)
                {
                    // Get all reviews for this worker
                    var allReviews = await dbContext.Reviews
                        .Where(r => r.WorkerId == worker.WorkerId)
                        .ToListAsync();

                    allReviews.Add(review); // Include the new review in calculation
                    
                    // Calculate average rating
                    worker.AverageRating = allReviews.Average(r => r.Rating);
                    dbContext.Workers.Update(worker);
                }

                await dbContext.SaveChangesAsync();

                return Ok(new { message = "Review submitted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error submitting review: {ex.Message}" });
            }
        }

        // ===== DTO for submitting review =====
        public class SubmitReviewDto
        {
            public Guid JobRequestId { get; set; }
            public int Rating { get; set; }
            public string ReviewText { get; set; }
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

        // ===== NEW: Get notifications for client =====
        [HttpGet]
        public async Task<IActionResult> NotifPage()
        {
            try
            {
                // ===== Get the logged-in user's ID from claims =====
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return RedirectToAction("LoginPage", "Account");
                }

                // ===== Get client profile for the logged-in user =====
                var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (client == null)
                {
                    return RedirectToAction("InfoPage_C");
                }

                // ===== Get notifications for this client =====
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
    }
}