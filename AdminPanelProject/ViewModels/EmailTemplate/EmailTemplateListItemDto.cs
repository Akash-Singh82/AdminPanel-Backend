using System;

namespace AdminPanelProject.ViewModels.EmailTemplate
{
    public record EmailTemplateListItemDto
    (
        Guid Id,
        string Key,
        string Title,
        string Subject,
        bool IsActive

    );
}
