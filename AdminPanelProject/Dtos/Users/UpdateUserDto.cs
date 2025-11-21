namespace AdminPanelProject.Dtos.Users
{
    public class UpdateUserDto
    {
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }=true;
        public string? ResetPassword { get; set; }
        // email not editable
        public bool IsImageChanged { get; set; }
    }
}
