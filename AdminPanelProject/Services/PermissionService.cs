using AdminPanelProject.Data;
using AdminPanelProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdminPanelProject.Services
{
    public class PermissionService:IPermissionService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<List<Permission>> GetPermissionsByUserIdAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return new List<Permission>();

            var roles = await _userManager.GetRolesAsync(user); // list of role names

            // Query RolePermissions -> include Permission
            var permissions = await _db.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => roles.Contains(rp.Role.Name))
                .Select(rp => rp.Permission)
                .Distinct()
                .ToListAsync();

            return permissions;
        }
    }
}
