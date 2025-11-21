using AdminPanelProject.Models;
using Microsoft.AspNetCore.Identity;

//using AdminPanelProject.Identit
using Microsoft.EntityFrameworkCore;

namespace AdminPanelProject.Data
{
    public class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();


            //await context.Database.MigrateAsync();

            if (!context.Permissions.Any())
            {
                var modules = new[] { "Users", "Roles", "EmailTemplates", "CMS", "FAQ", "AppConfig", "AuditLogs" };
                var actions = new[] { "List", "Add", "Edit", "Delete" };

                var allPermissions = new List<Permission>();
                foreach(var module in modules)
                {
                    foreach(var action in actions)
                    {
                        if (module == "AuditLogs" && action != "List")
                            continue;


                        allPermissions.Add(new Permission
                        {
                            Name = $"{module}.{action}",
                            Description = $"{action} permission for {module}",
                            CreatedBy = "System"
                        });
                    }
                }
                context.Permissions.AddRange(allPermissions);
                await context.SaveChangesAsync();
            }




            var rolesToEnsure = new[]
            {
                "SuperAdmin","Admin","Manager","User","Guest"
            };

            foreach(var roleName in rolesToEnsure)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if(role == null)
                {
                    role = new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        Description = $"{roleName} role seeded by system.",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow,
                    };
                    await roleManager.CreateAsync(role);
                }
                else if(string.IsNullOrEmpty(role.CreatedBy))
                {
                    role.CreatedBy = "System";
                    await roleManager.UpdateAsync(role);
                }
            }

            await context.SaveChangesAsync();

            // Listing all the permissions 
            var allPermissionsList = await context.Permissions.ToListAsync();


            //Assign Permissions to roles
            var superAdmin = await roleManager.FindByNameAsync("SuperAdmin");
            var admin = await roleManager.FindByNameAsync("Admin");
            var manager = await roleManager.FindByNameAsync("Manager");
            var user = await roleManager.FindByNameAsync("User");
            var guest = await roleManager.FindByNameAsync("Guest");

            void Assign(ApplicationRole role, IEnumerable<string> permissions)
            {
                foreach(var p in allPermissionsList.Where(x=> permissions.Contains(x.Name)))
                {
                    if (!context.RolePermissions.Any(rp => rp.RoleId == role.Id && rp.PermissionId == p.Id))
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = p.Id,
                            CreatedBy = "System"
                        });
                    }
                }
               
            }

            Assign(superAdmin, allPermissionsList.Select(p=>p.Name));
            Assign(admin, allPermissionsList.Where(p=>!p.Name.StartsWith("AuditLogs.")).Select(p=>p.Name));



            Assign(manager, allPermissionsList
    .Where(p =>
        (p.Name.StartsWith("Users.") && p.Name != "Users.Delete") ||
        (p.Name.StartsWith("EmailTemplates.") && p.Name != "EmailTemplates.Delete") ||
        (p.Name.StartsWith("CMS.") && p.Name != "CMS.Delete") ||
        (p.Name.StartsWith("FAQ.") && p.Name != "FAQ.Delete") ||
        (p.Name.StartsWith("AppConfig.") && p.Name != "AppConfig.Delete") ||
        p.Name == "AuditLogs.List")
    .Select(p => p.Name));



            // User → only FAQ.List + AppConfig.List
            Assign(user, new[] { "FAQ.List", "AppConfig.List" });

            // Guest → only FAQ.List
            Assign(guest, new[] { "FAQ.List" });

            await context.SaveChangesAsync();

            var superAdminUser = await userManager.FindByEmailAsync("akashsingh82874@gmail.com");
            if(superAdminUser != null)
            {


                var existingAddress = await context.Addresses
                    .FirstOrDefaultAsync(a=>a.UserId == superAdminUser.Id);
                if(existingAddress == null)
                {

            var address = new Address
            {
                UserId = superAdminUser.Id,
                Street = "Anandnagar",
                City = "Ahmedabad",
                State = "Gujarat",
                PostalCode = "802134",
                Country = "India",
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };
            context.Addresses.Add(address);
                }
            await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
             await context.SaveChangesAsync();
            }


        }
    }
}
