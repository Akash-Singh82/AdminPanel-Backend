using System.ComponentModel.DataAnnotations;

namespace AdminPanelProject.Dtos.Cms
{
    public class UpdateCmsDto
    {
        [Required] public string Title { get; set; } = string.Empty;
        public string MetaKeyword { get; set; } = string.Empty;
        public string MetaTitle { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
