namespace AdminPanelProject.Dtos.Users
{
    public class CreateUserDto
    {
        // Note: this DTO is used for JSON form, but for file upload we accept multipart
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public string Password { get; set; } = null!;

        public bool IsEmailConfirmed { get; set; } = false;
    }
}
