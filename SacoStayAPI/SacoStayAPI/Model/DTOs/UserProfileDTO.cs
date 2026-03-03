namespace SacoStayAPI.Model.DTOs
{
    public class UserProfileDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; } 
        public string? PhoneNumber { get; set; }

        public string? Job { get; set; }
        public string? LivingArea { get; set; }
        public string? Bio { get; set; }

    }
}
