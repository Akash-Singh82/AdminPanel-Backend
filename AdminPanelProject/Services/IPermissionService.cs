using AdminPanelProject.Models;

namespace AdminPanelProject.Services
{
    public interface IPermissionService
    {
        Task<List<Permission>> GetPermissionsByUserIdAsync(Guid userId);
    }
}
