namespace AdminPanelProject.Dtos.Users
{
    public class ChangePasswordDto
    {
        public string oldPassword { get; set; } = string.Empty;
        public string newPassword { get; set; } = string.Empty;
    }
}
