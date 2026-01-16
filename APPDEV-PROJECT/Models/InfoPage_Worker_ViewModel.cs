namespace APPDEV_PROJECT.Models
{
    public class InfoPage_Worker_ViewModel
    {
        // Personal Details
        public string FName { get; set; } = string.Empty;
        public string Mname { get; set; } = string.Empty;
        public string LName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Sex { get; set; } = string.Empty;

        // Contact Information
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;

        // Work Background
        public string Skill { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string Accomplishments { get; set; } = string.Empty;
    }
}
