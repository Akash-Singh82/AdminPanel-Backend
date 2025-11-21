namespace AdminPanelProject.ViewModels.EmailTemplate
{
    public class EmailTemplateCreateDto
    {
        public string Key { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsManualMail { get; set; } = false;
        public string Body { get; set; } = string.Empty;
    }
}
