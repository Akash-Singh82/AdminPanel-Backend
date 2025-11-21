using Microsoft.AspNetCore.Identity;

namespace AdminPanelProject.Models
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        //Audit Columns
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy{ get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy{ get; set;}

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
