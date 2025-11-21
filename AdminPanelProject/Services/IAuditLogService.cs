using AdminPanelProject.Models;

namespace AdminPanelProject.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string userName, string type, string activity);
        Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetPagedAsync(
            string? userName, string? type, string? activity,
            DateTime? fromDate, DateTime? toDate,
            int page, int pageSize, string? sortField, string? sortDirection);
    }
}
