// ===== NEW FILE: Registration View Model =====
// This captures registration form data before creating a User account
// Separate from the User entity to avoid exposing the PasswordHash

namespace APPDEV_PROJECT.Models
{
    public class RegisterViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty; // "Client" or "Worker"
    }
}
