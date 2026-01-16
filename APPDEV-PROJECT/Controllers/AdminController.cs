using Microsoft.AspNetCore.Mvc;
using APPDEV_PROJECT.Data;
using APPDEV_PROJECT.Models.Entities;
using APPDEV_PROJECT.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APPDEV_PROJECT.Controllers
{
    public class AdminController : Controller
    {
        private readonly HanapBuhayDBContext dbContext;

        public AdminController(HanapBuhayDBContext context)
        {
            dbContext = context;
        }

        // ===== Helper method to check if user is admin =====
        private bool IsAdmin()
        {
            var userType = User.FindFirst("UserType")?.Value;
            return userType == "Admin";
        }

        // ===== Redirect to login if not admin =====
        private IActionResult RedirectIfNotAdmin()
        {
            if (!User.Identity.IsAuthenticated || !IsAdmin())
            {
                return RedirectToAction("LoginPage", "Account");
            }
            return null;
        }

        // ===== Helper method to ensure admin user exists =====
        private async Task EnsureAdminExists()
        {
            var adminExists = await dbContext.Users.AnyAsync(u => u.Email == "admin@hanapbuhay.com" && u.UserType == "Admin");
            
            if (!adminExists)
            {
                // Generate a fresh hash for password "Admin@123"
                var freshHash = AuthenticationHelper.HashPassword("Admin@123");
                
                var adminUser = new User
                {
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Email = "admin@hanapbuhay.com",
                    UserType = "Admin",
                    PasswordHash = freshHash,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                dbContext.Users.Add(adminUser);
                await dbContext.SaveChangesAsync();
            }
        }

        public IActionResult DashboardPage_A()
        {
            var notAdminResult = RedirectIfNotAdmin();
            if (notAdminResult != null) return notAdminResult;

            return View();
        }

        public IActionResult Dashboard_A()
        {
            var notAdminResult = RedirectIfNotAdmin();
            if (notAdminResult != null) return notAdminResult;

            return View();
        }

        public async Task<IActionResult> ManageUsers()
        {
            var notAdminResult = RedirectIfNotAdmin();
            if (notAdminResult != null) return notAdminResult;

            try
            {
                var users = await dbContext.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error loading users: {ex.Message}");
                return View(new List<User>());
            }
        }

        // ===== NEW: Delete User (Soft Delete - Deactivate) =====
        [HttpPost]
        [Route("Admin/DeleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            try
            {
                var notAdminCheck = RedirectIfNotAdmin();
                if (notAdminCheck != null)
                {
                    return Unauthorized(new { message = "Admin access required" });
                }

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Prevent deleting the main admin user
                if (user.Email == "admin@hanapbuhay.com")
                {
                    return BadRequest(new { message = "Cannot delete the main admin user" });
                }

                // Soft delete: Set IsActive to false
                user.IsActive = false;

                dbContext.Users.Update(user);
                await dbContext.SaveChangesAsync();

                return Ok(new { message = "User has been deactivated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error deleting user: {ex.Message}" });
            }
        }

        public IActionResult AppMan_A()
        {
            var notAdminResult = RedirectIfNotAdmin();
            if (notAdminResult != null) return notAdminResult;

            return View();
        }

        public async Task<IActionResult> Report_A()
        {
            var notAdminResult = RedirectIfNotAdmin();
            if (notAdminResult != null) return notAdminResult;

            try
            {
                var reports = await dbContext.Reports
                    .Include(r => r.Reporter)
                    .Include(r => r.ReportedWorker)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return View(reports);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error loading reports: {ex.Message}");
                return View(new List<Report>());
            }
        }

        public async Task<IActionResult> ViewDetails_A(Guid reportId)
        {
            var notAdminResult = RedirectIfNotAdmin();
            if (notAdminResult != null) return notAdminResult;

            try
            {
                var report = await dbContext.Reports
                    .Include(r => r.Reporter)
                    .Include(r => r.ReportedWorker)
                    .FirstOrDefaultAsync(r => r.ReportId == reportId);

                if (report == null)
                {
                    return NotFound();
                }

                return View(report);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error loading report details: {ex.Message}");
                return RedirectToAction("Report_A");
            }
        }

        // ===== NEW: Submit Report API (from client/worker) =====
        [HttpPost]
        [Route("api/admin/submit-report")]
        public async Task<IActionResult> SubmitReport([FromBody] SubmitReportDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var reporterId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var report = new Report
                {
                    ReportId = Guid.NewGuid(),
                    ReporterId = reporterId,
                    ReportedWorkerId = dto.ReportedWorkerId,
                    Reason = dto.Reason,
                    ContentType = dto.ContentType,
                    Description = dto.Description,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                dbContext.Reports.Add(report);
                await dbContext.SaveChangesAsync();

                return Ok(new { message = "Report submitted successfully", reportId = report.ReportId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error submitting report: {ex.Message}" });
            }
        }

        // ===== NEW: Update Report Status (Admin) =====
        [HttpPost]
        [Route("api/admin/update-report/{reportId}")]
        public async Task<IActionResult> UpdateReportStatus(Guid reportId, [FromBody] UpdateReportDto dto)
        {
            try
            {
                var notAdminCheck = RedirectIfNotAdmin();
                if (notAdminCheck != null) return Unauthorized(new { message = "Admin access required" });

                var report = await dbContext.Reports.FirstOrDefaultAsync(r => r.ReportId == reportId);

                if (report == null)
                {
                    return NotFound(new { message = "Report not found" });
                }

                report.Status = dto.Status;
                report.AdminNotes = dto.AdminNotes;
                report.UpdatedAt = DateTime.Now;

                dbContext.Reports.Update(report);
                await dbContext.SaveChangesAsync();

                return Ok(new { message = "Report updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error updating report: {ex.Message}" });
            }
        }

        // ===== NEW: Delete Report (Admin) =====
        [HttpPost]
        [Route("api/admin/delete-report/{reportId}")]
        public async Task<IActionResult> DeleteReport(Guid reportId)
        {
            try
            {
                var notAdminCheck = RedirectIfNotAdmin();
                if (notAdminCheck != null) return Unauthorized(new { message = "Admin access required" });

                var report = await dbContext.Reports.FirstOrDefaultAsync(r => r.ReportId == reportId);

                if (report == null)
                {
                    return NotFound(new { message = "Report not found" });
                }

                dbContext.Reports.Remove(report);
                await dbContext.SaveChangesAsync();

                return Ok(new { message = "Report deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error deleting report: {ex.Message}" });
            }
        }

        // ===== DTO for submitting reports =====
        public class SubmitReportDto
        {
            public Guid? ReportedWorkerId { get; set; }
            public string Reason { get; set; }
            public string ContentType { get; set; }
            public string Description { get; set; }
        }

        // ===== DTO for updating reports =====
        public class UpdateReportDto
        {
            public string Status { get; set; }
            public string AdminNotes { get; set; }
        }
    }
}
