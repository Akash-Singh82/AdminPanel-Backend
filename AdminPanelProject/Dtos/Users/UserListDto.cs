namespace AdminPanelProject.Dtos.Users
{
    // Dtos/Users/UserListDto.cs
    public class UserListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!; // First + Last
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
    }

}
