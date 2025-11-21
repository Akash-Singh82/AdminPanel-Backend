using AdminPanelProject.Data;
using AdminPanelProject.Dtos.Roles;
using AdminPanelProject.Models;
using AdminPanelProject.ViewModels;
//using AdminPanelProject.ViewModels.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdminPanelProject.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public RoleService(RoleManager<ApplicationRole> roleManager, ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<string> countAsync()
        {
            int count = await _roleManager.Roles.CountAsync();
            return count.ToString();
        }

        public async Task<PagedResult<RoleListDto>> GetRolesAsync(
     string? name,
     string? description,
     bool? isActive,
     int pageNumber,
     int pageSize,
     string? sortField = "Name",
     string? sortDirection = "asc")
        {
            var query = _roleManager.Roles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(r => r.Name.Contains(name));

            if (!string.IsNullOrWhiteSpace(description))
                query = query.Where(r => r.Description != null && r.Description.Contains(description));

            if (isActive.HasValue)
                query = query.Where(r => r.IsActive == isActive.Value);

            // Sorting
            sortField = sortField?.Trim().ToLower();
            sortDirection = sortDirection?.Trim().ToLower();

            query = (sortField, sortDirection) switch
            {
                ("description", "desc") => query.OrderByDescending(r => r.Description),
                ("description", "asc") => query.OrderBy(r => r.Description),

                ("isactive", "desc") => query.OrderByDescending(r => r.IsActive),
                ("isactive", "asc") => query.OrderBy(r => r.IsActive),

                ("createdon", "desc") => query.OrderByDescending(r => r.CreatedOn),
                ("createdon", "asc") => query.OrderBy(r => r.CreatedOn),

                ("createdby", "desc") => query.OrderByDescending(r => r.CreatedBy),
                ("createdby", "asc") => query.OrderBy(r => r.CreatedBy),

                ("name", "desc") => query.OrderByDescending(r => r.Name),
                _ => query.OrderBy(r => r.Name) // default
            };


            var totalCount = await query.CountAsync();

            // Calculate total pages
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Adjust pageNumber if it exceeds totalPages
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = totalPages;
            }

            var roles = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RoleListDto
                {
                    Id = r.Id,
                    Name = r.Name!,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    CreatedOn = r.CreatedOn,
                    CreatedBy = r.CreatedBy
                })
                .ToListAsync();

            return new PagedResult<RoleListDto>
            {
                Items = roles,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }


        public async Task<RoleDetailsDto?> GetRoleByIdAsync(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                return null;

            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Include(rp => rp.Permission)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description
                })
                .ToListAsync();

            return new RoleDetailsDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                PermissionIds = permissions.Select(p => p.Id).ToList(),
                Permissions = permissions
            };
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateRoleAsync(RoleDto dto, string createdBy)
        {
            var normalizedName = dto.Name.Trim().ToUpper();
            var existingRole = await _roleManager.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedName);

            var role = new ApplicationRole
            {
                Name = dto.Name.Trim(),
                NormalizedName = dto.Name.ToUpper(),
                Description = dto.Description?.Trim(),
                IsActive = dto.IsActive,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded) 
                return (false, string.Join(";", result.Errors.Select(e=>e.Description)));

            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                foreach (var permissionId in dto.PermissionIds)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedBy = createdBy,
                        CreatedOn = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }
            return (true, null);
        }


        public async Task<(bool Success, string? ErrorMessage, string? RoleName)> UpdateRoleAsync(Guid id, RoleDto dto, string modifiedBy)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                return (false, "Role not found", null);

            // 🔒 Prevent updating SuperAdmin
            if (string.Equals(role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Cannot modify protected role: {role.Name}", null);
            }

            try
            {
                // Update role properties
                role.Name = dto.Name;
                role.Description = dto.Description;
                role.IsActive = dto.IsActive;
                role.ModifiedBy = modifiedBy;
                role.ModifiedOn = DateTime.UtcNow;

                await _roleManager.UpdateAsync(role);

                // Replace permissions
                var existingPermissions = _context.RolePermissions.Where(rp => rp.RoleId == role.Id);
                _context.RolePermissions.RemoveRange(existingPermissions);

                foreach (var permissionId in dto.PermissionIds)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedBy = modifiedBy,
                        CreatedOn = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return (true, null, role.Name);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while updating the role: {ex.Message}", role.Name);
            }
        }


        public async Task<bool> ToggleStatusAsync(Guid id, string modifiedBy)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return false;

            role.IsActive = !role.IsActive;
            role.ModifiedBy = modifiedBy;
            role.ModifiedOn = DateTime.UtcNow;

            await _roleManager.UpdateAsync(role);
            await _context.SaveChangesAsync();
            return true;
        }




        public async Task<(bool Success, string? ErrorMessage)> DeleteRoleAsync(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return (false, "Role not found");

            var protectedRoles = new[] { "SuperAdmin", "Admin" };
            if (protectedRoles.Any(r => string.Equals(role.Name, r, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, $"Cannot delete protected role: {role.Name}");
            }


            // If identity uses AspNetUserRoles table
            var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == role.Id);
            if (hasUsers)
            {
                return (false, "Cannot delete role - one or more users are assigned to this role.");
            }


            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded) return (false, "Failed to delete role.");


            return (true, null);
        }

        public async Task<bool> RoleExistsAsync(Guid roleId)
        {
            return await _roleManager.Roles.AnyAsync(r => r.Id == roleId);
        }



    }
}