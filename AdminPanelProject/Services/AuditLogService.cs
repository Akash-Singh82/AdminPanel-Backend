using AdminPanelProject.Data;
using AdminPanelProject.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminPanelProject.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _db;

        public AuditLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string userName, string type, string activity)
        {
            var log = new AuditLog
            {
                UserName = userName,
                Type = type,
                Activity = activity,
                Timestamp = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<(IEnumerable<AuditLog>Logs, int TotalCount)> GetPagedAsync(string? userName, string? type, string? activity,
            DateTime? fromDate, DateTime? toDate,
            int page, int pageSize, string? sortField="Timestamp", string? sortDirection="desc")
        {
            try
            {


                var query = _db.AuditLogs.AsQueryable();
                if (!string.IsNullOrWhiteSpace(userName))
                    query = query.Where(x => x.UserName.Contains(userName));

                if (!string.IsNullOrWhiteSpace(type))
                    query = query.Where(x => x.Type.Contains(type));

                if (!string.IsNullOrWhiteSpace(activity))
                    query = query.Where(x => x.Activity.Contains(activity));

                if (fromDate.HasValue)
                    query = query.Where(x => x.Timestamp >= fromDate);

                if (toDate.HasValue)
                    query = query.Where(x => x.Timestamp <= toDate);

                bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

                query = sortField?.ToLower() switch
                {
                    "username" => descending ? query.OrderByDescending(x => x.UserName) : query.OrderBy(x => x.UserName),
                    "type" => descending ? query.OrderByDescending(x => x.Type) : query.OrderBy(x => x.Type),
                    "activity" => descending ? query.OrderByDescending(x => x.Activity) : query.OrderBy(x => x.Activity),
                    "timestamp" or _ => descending ? query.OrderByDescending(x => x.Timestamp) : query.OrderBy(x => x.Timestamp),
                };

                var total = await query.CountAsync();
                var logs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (logs, total);

            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve audit logs from the database.", ex);
            }

        }
    }
}
