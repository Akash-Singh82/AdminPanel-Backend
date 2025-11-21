using System.ComponentModel.DataAnnotations;

namespace AdminPanelProject.Models
{
    public class Permission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; } 

        public string? Description { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
