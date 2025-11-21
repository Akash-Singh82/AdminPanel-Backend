namespace AdminPanelProject.Dtos.Roles
{
    public class RoleDetailsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public List<Guid> PermissionIds { get; set; } = new();

        public List<PermissionDto> Permissions { get; set; } = new();
    }
}
