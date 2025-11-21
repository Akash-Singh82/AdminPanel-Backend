using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AdminPanelProject.Data;
using AdminPanelProject.Models;

namespace AdminPanelProject.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class HasPermissionAttribute : Attribute,IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;

        public HasPermissionAttribute(params string[] permissions)
        {
            _permissions = permissions;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();


            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var appUser = await userManager.FindByIdAsync(userId);
            if(appUser == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var roles = await userManager.GetRolesAsync(appUser);

            if (roles == null || roles.Count == 0)
            {
                context.Result = new ForbidResult();
                return;
            }

            // get permissions for those roles
            var hasPermission = await db.RolePermissions
                .Include(rp => rp.Permission)
                .Include(rp => rp.Role)
                .AnyAsync(rp => roles.Contains(rp.Role.Name) &&  _permissions.Contains(rp.Permission.Name));

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                return;
            }


        }

    }
}
