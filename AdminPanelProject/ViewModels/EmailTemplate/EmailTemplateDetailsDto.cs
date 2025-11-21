namespace AdminPanelProject.ViewModels.EmailTemplate
{
    public class EmailTemplateDetailsDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
        public bool IsActive { get; set; }
        public bool IsManualMail { get; set; }
        public string Body { get; set; } = string.Empty;
    }

}
