using System;
using System.ComponentModel.DataAnnotations;

namespace AdminPanelProject.Models
{
    public class EmailTemplate
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = null!; // unique key e.g. "USER_CONFIRMATION"

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(250)]
        public string Subject { get; set; } = null!;

        [StringLength(250)]
        public string? FromEmail { get; set; }

        [StringLength(200)]
        public string? FromName { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsManualMail { get; set; } = false;

        // HTML body stored as string (CKEditor HTML)
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // 👇 Add these new audit fields
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
