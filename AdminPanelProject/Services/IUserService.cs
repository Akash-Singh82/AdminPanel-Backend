using AdminPanelProject.Dtos.Users;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelProject.Services
{
    public interface IUserService
    {
      



        Task<(List<UserListDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize,
        string? name, string? email, string? phone, Guid? roleId, bool? isActive, string? sortBy, string? sortDirection);

        Task<UserDetailsDto?> GetByIdAsync(Guid id);
        Task<(bool Success, string? Error, string? ErrorCode)> CreateAsync(CreateUserDto dto, IFormFile? profileImage, string createdBy);
        Task<(bool Success, string? Error, string? ErrorCode)> UpdateAsync(Guid id, UpdateUserDto dto, IFormFile? profileImage, string updatedBy);
        Task<(bool Success, string? Error, string? ErrorCode)> DeleteAsync(Guid id);
        Task<FileStreamResult> ExportCsvAsync(int pageNumber, int pageSize, string? name, string? email, string? phone, Guid? roleId, bool? isActive, string? sortBy, string? sortDirection);
        Task<List<(Guid Id, string Name)>> GetRolesSimpleAsync();

        Task<(bool Success, string? ErrorMessage)> ToggleStatusAsync(Guid id, string modifiedBy);
        Task<string> countAsync();

        Task<(bool Success, string? Error, string? ErrorCode)> ChangePasswordAsync(Guid id, string currentPassword, string newPassword);
    }
}
