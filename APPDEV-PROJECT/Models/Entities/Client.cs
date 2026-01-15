// this is where were going to declare the properties that will represent the entities that we will be having

namespace APPDEV_PROJECT.Models.Entities
{
    public class Client
    {
        public Guid ClientId { get; set; }
        public string LName { get; set; }

        public string FName { get; set; }
        public string Mname { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Sex { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string FullName => $"{FName} {LName}";

    }
}
