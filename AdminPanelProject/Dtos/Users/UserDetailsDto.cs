namespace AdminPanelProject.Dtos.Users
{
    // Dtos/Users/UserDetailsDto.cs
    public class UserDetailsDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string? ProfileImagePath { get; set; }
    }

}
