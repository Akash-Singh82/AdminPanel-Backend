using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AdminPanelProject.Models
{
    [Index(nameof(CreatedOn), IsDescending = new[] { true })]
    // Ascending index
    [Index(nameof(CreatedOn), IsDescending = new[] { false })]
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        //Audit Columns
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        public string? ProfileImagePath { get; set; }

        // Navigation property for one-to-many relationsip

        public virtual List<Address>? Addresses { get; set; } = new List<Address>();
    }
}
