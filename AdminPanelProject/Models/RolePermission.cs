using System.ComponentModel.DataAnnotations.Schema;

namespace AdminPanelProject.Models
{
    public class RolePermission
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Role")]
        public Guid RoleId { get; set; }
        public ApplicationRole Role { get; set; } = null!;

        [ForeignKey("Permission")]
        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        // Audit
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }
}
