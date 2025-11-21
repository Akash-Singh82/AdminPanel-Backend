using AdminPanelProject.Models;

namespace AdminPanelProject.Dtos.Roles
{
    public class RoleListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        // Navigation property
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
