



using AdminPanelProject.ViewModels;
using AdminPanelProject.ViewModels.EmailTemplate;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;


namespace AdminPanelProject.Services
{
    public interface IEmailTemplateService
    {
        Task<PagedResult<EmailTemplateListItemDto>> GetPagedAsync(int pageNumber,
                                                     int pageSize,
                                                     string? key,
                                                     string? title,
                                                     string? subject,
                                                     bool? isActive,
                                                     string? sortDirection,
                                                     string? sortField);
        Task<EmailTemplateDetailsDto?> GetByIdAsync(Guid id);
        //Task<EmailTemplateDetailsDto> CreateAsync(EmailTemplateCreateDto dto, string createdBy);
        //Task<EmailTemplateDetailsDto?> UpdateAsync(EmailTemplateEditDto dto, string updatedBy);
        //Task<bool> DeleteAsync(Guid id);

        Task<(bool Success, string? Error, string? ErrorCode, EmailTemplateDetailsDto? Result)> CreateAsync(EmailTemplateCreateDto dto, string createdBy);
        Task<(bool Success, string? Error, string? ErrorCode, EmailTemplateDetailsDto? Result)> UpdateAsync(EmailTemplateEditDto dto, string updatedBy);
        Task<(bool Success, string? Error, string? ErrorCode)> DeleteAsync(Guid id);

        Task<(bool Success, string? message)> ToggleStatusAsync(Guid id, string updatedBy);
        Task<FileContentResult> ExportCsvAsync(string? key, string? title, string? subject, bool? isActive);
    }
}
