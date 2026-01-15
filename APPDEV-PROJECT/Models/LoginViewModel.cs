// ===== NEW FILE: Login View Model =====
// This captures login form data for authentication

namespace APPDEV_PROJECT.Models
{
    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty; // "Client" or "Worker"
    }
}
