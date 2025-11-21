using System;

namespace AdminPanelProject.Models
{
    public class CmsEntity
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;      // immutable after create
        public string Title { get; set; } = string.Empty;
        public string MetaKeyword { get; set; } = string.Empty;
        public string MetaTitle { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;  // HTML from CKEditor
        public bool IsActive { get; set; }

        // Audit fields
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
