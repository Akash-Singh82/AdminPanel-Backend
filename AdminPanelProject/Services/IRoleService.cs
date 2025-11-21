using AdminPanelProject.Dtos.Roles;
using AdminPanelProject.ViewModels;
//using AdminPanelProject.ViewModels.Roles;
using Microsoft.AspNetCore.Identity;

namespace AdminPanelProject.Services
{
    public interface IRoleService
    {


        Task<PagedResult<RoleListDto>> GetRolesAsync(string? name, string? description, bool? isActive, int pageNumber, int pageSize ,string? sortField,
     string? sortDirection);
        Task<RoleDetailsDto?> GetRoleByIdAsync(Guid id);
        Task<(bool Success, string? ErrorMessage)> CreateRoleAsync(RoleDto dto, string createdBy);
        Task<(bool Success, string? ErrorMessage, string? RoleName)> UpdateRoleAsync(Guid id, RoleDto dto, string modifiedBy);
        Task<(bool Success, string? ErrorMessage)> DeleteRoleAsync(Guid id);

        Task<bool> ToggleStatusAsync(Guid id, string modifiedBy);

        Task<string> countAsync();
        Task<bool> RoleExistsAsync(Guid roleId);
    }
}
