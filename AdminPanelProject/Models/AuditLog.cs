// Models/AuditLog.cs
using System;

namespace AdminPanelProject.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;  // e.g. Create, Update, Delete, View
        public string Activity { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
