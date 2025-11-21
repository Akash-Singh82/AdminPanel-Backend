

using AdminPanelProject.Dtos.Cms;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelProject.Services
{
    public interface ICmsService
    {
        Task<(IReadOnlyList<CmsDto> items, int total)> GetPagedAsync(
            int pageNumber, int pageSize,
            string? title, string? key, string? metaKeyword, bool? isActive, string sortField, string sortDirection);

        Task<CmsDto?> GetByIdAsync(Guid id);
        Task<(bool Success, string? Error, string? ErrorCode)> CreateAsync(CreateCmsDto dto, string createdBy);

        Task<bool> UpdateAsync(Guid id, UpdateCmsDto dto, string modifiedBy);
        Task<bool> DeleteAsync(Guid id);
        Task<FileContentResult> ExportCsvAsync(string? title, string? key, string? metaKeyword, bool? isActive);
        Task<bool> ToggleStatusAsync(Guid id, string modifiedBy);
    }
}
