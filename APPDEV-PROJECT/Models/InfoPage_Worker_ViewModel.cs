namespace APPDEV_PROJECT.Models
{
    public class InfoPage_Worker_ViewModel
    {
        // Personal Details
        public string FName { get; set; }
        public string Mname { get; set; }
        public string LName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Sex { get; set; }

        // Contact Information
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        // Work Background
        public string Skill { get; set; }
        public int YearsOfExperience { get; set; }
        public string Accomplishments { get; set; }
    }
}
