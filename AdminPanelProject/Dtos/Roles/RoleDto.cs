namespace AdminPanelProject.Dtos.Roles
{
    public class RoleDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public List<Guid> PermissionIds { get; set; } = new();
    }
}
