

using AdminPanelProject.Data;
using AdminPanelProject.Dtos.Users;
using AdminPanelProject.Models;
using AdminPanelProject.ViewModels;
//using AdminPanelProject.ViewModels.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Memory;
//VJxB % 9KhJ* pH2nY

namespace AdminPanelProject.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<UserService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IMemoryCache _cache;

       
        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<UserService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;

        }


        public async Task<string>countAsync()
        {
            int count = await _userManager.Users.CountAsync();
            return count.ToString();
        }

        private record CachedUserListResult(List<UserListDto> Items, int TotalCount);

        private const string UserListCacheKeysKey = "UserListCacheKeys";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        private string BuildUserListCacheKey(
            int pageNumber,
            int pageSize, 
            string? name, 
            string? email,
            string? phone,
            Guid? roleId,
            bool? isActive,
            string? sortBy,
            string? sortDirection)
        {
            string kv(string? v) => string.IsNullOrWhiteSpace(v)?"-":v!.Trim().ToLowerInvariant();
            return $"UserList:pg={pageNumber}:ps={pageSize}:n={kv(name)}:e={kv(email)}:ph={kv(phone)}:role={(roleId?.ToString() ?? "-")}:active={(isActive.HasValue ? (isActive.Value ? "1" : "0") : "-")}:sby={kv(sortBy)}:sdir={kv(sortDirection)}";

        }

        private void RegisterUserListCacheKey(string key)
        {
            if(!_cache.TryGetValue(UserListCacheKeysKey, out HashSet<string>? set) || set is null)
            {
                set = new HashSet<string>();
            }
            if (set.Add(key))
            {
                _cache.Set(UserListCacheKeysKey, set, TimeSpan.FromHours(6)); // registry TTL longer than entries
            }
        }

        public void ClearUserListCaches()
        {
            if (_cache.TryGetValue(UserListCacheKeysKey, out HashSet<string>? set) && set != null)
            {
                foreach (var key in set)
                {
                    _cache.Remove(key);
                }
            // remove registry itself
            _cache.Remove(UserListCacheKeysKey);
                
            }
        }
        public async Task<(List<UserListDto> Items, int TotalCount)> GetPagedAsync(
    int pageNumber,
    int pageSize,
    string? name,
    string? email,
    string? phone,
    Guid? roleId,
    bool? isActive,
    string? sortBy,
    string? sortDirection
)
        {
            _logger.LogInformation($"Sorting: {sortBy} {sortDirection}");

            try
            {
                var cacheKey = BuildUserListCacheKey(pageNumber, pageSize, name, email, phone, roleId, isActive, sortBy, sortDirection);
                if(_cache.TryGetValue(cacheKey, out CachedUserListResult? cached))
                {
                    _logger.LogInformation("Returning user list from cache.");
                    return (cached!.Items, cached!.TotalCount);
                }
                // ✅ Build base query (includes roles directly for sorting)
                var usersQuery =
                    from u in _userManager.Users
                    join ur in _context.UserRoles on u.Id equals ur.UserId into userRoleJoin
                    from ur in userRoleJoin.DefaultIfEmpty()
                    join r in _roleManager.Roles on ur.RoleId equals r.Id into roleJoin
                    from r in roleJoin.DefaultIfEmpty()
                    select new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.PhoneNumber,
                        u.IsActive,
                        u.CreatedOn,
                        RoleId = r.Id,
                        RoleName = r.Name
                    };

                //  Apply filters
                if (!string.IsNullOrWhiteSpace(name))
                {
                    string nameFilter = name.Trim();
                    usersQuery = usersQuery.Where(u =>
                        (u.FirstName + " " + (u.LastName ?? "")).Contains(nameFilter));
                }

                if (!string.IsNullOrWhiteSpace(email))
                    usersQuery = usersQuery.Where(u => u.Email.Contains(email));

                if (!string.IsNullOrWhiteSpace(phone))
                    usersQuery = usersQuery.Where(u => u.PhoneNumber != null && u.PhoneNumber.StartsWith(phone));

                if (isActive.HasValue)
                    usersQuery = usersQuery.Where(u => u.IsActive == isActive.Value);

                if (roleId.HasValue)
                    usersQuery = usersQuery.Where(u => u.RoleId == roleId.Value);

                //  Count total after filters
                var totalCount = await usersQuery.CountAsync();

                //  Sorting
                bool isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
                sortBy = sortBy?.ToLower() ?? "createdon";

                usersQuery = sortBy switch
                {
                    "name" => isDescending
                        ? usersQuery.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName)
                        : usersQuery.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),

                    "email" => isDescending
                        ? usersQuery.OrderByDescending(u => u.Email)
                        : usersQuery.OrderBy(u => u.Email),

                    "phone" => isDescending
                        ? usersQuery.OrderByDescending(u => u.PhoneNumber)
                        : usersQuery.OrderBy(u => u.PhoneNumber),

                    "isactive" or "status" => isDescending
                        ? usersQuery.OrderByDescending(u => u.IsActive)
                        : usersQuery.OrderBy(u => u.IsActive),

                    "role" => isDescending
                        ? usersQuery.OrderByDescending(u => u.RoleName)
                        : usersQuery.OrderBy(u => u.RoleName),

                    _ => isDescending
                        ? usersQuery.OrderByDescending(u => u.CreatedOn ?? DateTime.MinValue)
                        : usersQuery.OrderBy(u => u.CreatedOn ?? DateTime.MinValue)
                };

                // Pagination
                var users = await usersQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!users.Any())
                {
                    var emptyResult = new CachedUserListResult(new List<UserListDto>(), totalCount);
                    var optionsEmpty = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _cacheExpiration
                    };
                    _cache.Set(cacheKey, emptyResult, optionsEmpty);
                    RegisterUserListCacheKey(cacheKey);
                    return (emptyResult.Items, emptyResult.TotalCount);
                }

                // Map to DTOs (roles already available)
                var items = users.Select(u => new UserListDto
                {
                    Id = u.Id,
                    Name = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.RoleName ?? string.Empty,
                    IsActive = u.IsActive
                }).ToList();
                var result = new CachedUserListResult(items, totalCount);

                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration,
                };
                _cache.Set(cacheKey, result, options);
                RegisterUserListCacheKey(cacheKey);
                return (result.Items, result.TotalCount);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("User list request was cancelled by client.");
                return (new List<UserListDto>(), 0);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("User list request operation was cancelled.");
                return (new List<UserListDto>(), 0);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update exception while fetching users list.");
                throw;
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error while fetching users list.");
                throw new ApplicationException("A database error occurred while retrieving users.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetPagedAsync.");
                throw new ApplicationException("An unexpected error occurred while retrieving users.", ex);
            }
        }






        public async Task<UserDetailsDto?> GetByIdAsync(Guid id)
        {
            var u = await _userManager.FindByIdAsync(id.ToString());
            if (u == null) return null;

            var roleId = await _context.UserRoles.Where(ur => ur.UserId == u.Id).Select(ur => (Guid?)ur.RoleId).FirstOrDefaultAsync();
            var roleName = roleId.HasValue ? (await _roleManager.FindByIdAsync(roleId.Value.ToString()))?.Name : null;

            // ✅ Build full URL if image exists
            string? imageUrl = null;
            if (!string.IsNullOrEmpty(u.ProfileImagePath))
            {
                var request = _httpContextAccessor?.HttpContext?.Request;
                if (request != null)
                {
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    imageUrl = $"{baseUrl}{u.ProfileImagePath}";
                }
            }

            return new UserDetailsDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email!,
                PhoneNumber = u.PhoneNumber,
                RoleId = roleId,
                RoleName = roleName,
                IsActive = u.IsActive,
                IsEmailConfirmed = u.EmailConfirmed,
                ProfileImagePath = u.ProfileImagePath
            };
        }


        private async Task<string?> SaveProfileImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            string[] allowedExtensions = { ".jpg", ".png", ".jfif", ".jpeg", ".tif" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Invalid file type. Allowed types: JPG, PNG, JPEG, JFIF, TIF.", "InvalidFileType");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 5MB limit.", "FileSizeExceeded");

            var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "profiles");

            if (!Directory.Exists(uploads)) 
                Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploads, fileName);
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);
            // return relative path
            return $"/uploads/profiles/{fileName}";
        }


        public async Task<(bool Success, string? Error, string? ErrorCode)> CreateAsync(CreateUserDto dto, IFormFile? profileImage, string createdBy)
        {
            var exists = await _userManager.FindByEmailAsync(dto.Email);
            if (exists != null)
                return (false, "A user with this email already exists.", "Duplicate");

            var role = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
            if (role == null)
                return (false, "Invalid role specified.", "InvalidRole");

            // 🚫 Block creation with SuperAdmin
            if (string.Equals(role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                var user1 = _httpContextAccessor.HttpContext?.User;
                var userId = user1?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var email = user1?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                ApplicationUser? creator = null;
                if (!string.IsNullOrEmpty(userId))
                    creator = await _userManager.FindByIdAsync(userId);
                if (creator == null && !string.IsNullOrEmpty(email))
                    creator = await _userManager.FindByEmailAsync(email);

                if (creator == null)
                    return (false, "Unauthorized action: user not found.", "Unauthorized");

                var creatorRoles = await _userManager.GetRolesAsync(creator);
                if (!creatorRoles.Any(r => string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, "Only a SuperAdmin can create another SuperAdmin.", "ProtectedRole");
                }
            }


            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                IsActive = dto.IsActive,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = createdBy
            };
            try
            {
                var imgPath = await SaveProfileImageAsync(profileImage);
                if (imgPath != null) user.ProfileImagePath = imgPath;
            }
            catch(ArgumentException ex)
            {
                var cleanMessage = ex.Message.Split("(")[0].Trim();
                return (false, cleanMessage, ex.ParamName);
            }

            var res = await _userManager.CreateAsync(user, dto.Password);
            if (!res.Succeeded)
                return (false, string.Join(", ", res.Errors.Select(e => e.Description)), "IdentityError");

            if (dto.IsEmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, token);
            }

            await _userManager.AddToRoleAsync(user, role.Name);
            try
            {
            ClearUserListCaches();
            }
            catch
            {
                _logger.LogWarning("Failed to clear user list cache.");
            }
            return (true, null, null);
        }


        public async Task<(bool Success, string? Error, string? ErrorCode)> DeleteAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return (false, "User not found", "NotFound");

            var roles = await _userManager.GetRolesAsync(user);
            //if (roles.Any(r => string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            //    return (false, "Cannot delete a user with the SuperAdmin role.", "ProtectedRole");

            DeletePhysicalImageIfExists(user.ProfileImagePath);

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return (false, "Failed to delete user.", "DeleteFailed");
            try
            {
            ClearUserListCaches();
            }
            catch
            {
                _logger.LogWarning("Failed to clear user list cache.");
            }
            return (true, null, null);
        }


        public async Task<(bool Success, string? Error, string? ErrorCode)> UpdateAsync(Guid id, UpdateUserDto dto, IFormFile? profileImage, string modifiedBy)
        {
            
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return (false, "User not found", "NotFound");

                        
  
                // Get role being assigned
                var newRole = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
                if (newRole == null)
                    return (false, "Invalid role specified.", "InvalidRole");


                if (string.Equals(newRole.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    var httpUser = _httpContextAccessor.HttpContext?.User;
                    var userId = httpUser?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var email = httpUser?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                    ApplicationUser? modifier = null;

                    if (!string.IsNullOrEmpty(userId))
                        modifier = await _userManager.FindByIdAsync(userId);

                    if (modifier == null && !string.IsNullOrEmpty(email))
                        modifier = await _userManager.FindByEmailAsync(email);

                    if (modifier == null)
                        return (false, "Unauthorized action: user not found.", "Unauthorized");

                    var modifierRoles = await _userManager.GetRolesAsync(modifier);
                    if (!modifierRoles.Any(r => string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase)))
                    {
                        return (false, "Only a SuperAdmin can assign or update a SuperAdmin role.", "ProtectedRole");
                    }
                }
            

            // 🚫 Block updates to SuperAdmin role
            //if (string.Equals(newRole.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            //    return (false, "Cannot assign or update any user to the SuperAdmin role.", "ProtectedRole");

            // Update profile data
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.IsActive = dto.IsActive;
            user.ModifiedOn = DateTime.UtcNow;
            user.ModifiedBy = modifiedBy;
            //user.EmailConfirmed = dto.IsEmailConfirmed;

            // Handle profile image
            if ( profileImage != null )
            {
                try
                {

                var imgPath = await SaveProfileImageAsync(profileImage);
                DeletePhysicalImageIfExists(user.ProfileImagePath);

                if (imgPath != null)
                    user.ProfileImagePath = imgPath;
                }
                catch (ArgumentException ex)
                {
                    var cleanMessage = ex.Message.Split("(")[0].Trim();
                    return (false, cleanMessage, ex.ParamName);
                }

            }


            // Save base user changes
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)), "UpdateFailed");

            // Update role
        

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            await _userManager.AddToRoleAsync(user, newRole.Name);
            

            // Handle password reset if requested
            if (!string.IsNullOrWhiteSpace(dto.ResetPassword))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, dto.ResetPassword);

                if (!passwordResult.Succeeded)
                    return (false, string.Join(", ", passwordResult.Errors.Select(e => e.Description)), "PasswordResetFailed");
            }
            ClearUserListCaches();
            return (true, null, null);
        }




        private void DeletePhysicalImageIfExists(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;

            // imagePath stored like "/uploads/profiles/abcd.jpg"
            // compute physical path:
            var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var relative = imagePath.TrimStart('/', '\\'); // "uploads/profiles/abcd.jpg"
            var physical = Path.Combine(wwwroot, relative);

            if (File.Exists(physical))
            {
                try { File.Delete(physical); }
                catch
                {
                    // handle/log error, but don't throw in delete pipeline unless you want to fail deletion
                }
            }
        }



        public async Task<FileStreamResult> ExportCsvAsync(int pageNumber, int pageSize, string? name, string? email, string? phone, Guid? roleId, bool? isActive, string? sortBy, string? sortDirection)
        {
            var (items, total) = await GetPagedAsync(pageNumber, pageSize, name, email, phone, roleId, isActive, null, null);
            var sb = new StringBuilder();
            sb.AppendLine("Name,Email,PhoneNumber,Role,Status");
            foreach (var r in items)
            {
                var line = $"\"{r.Name}\",\"{r.Email}\",\"{r.PhoneNumber ?? ""}\",\"{r.Role}\",\"{(r.IsActive ? "Active" : "In Active")}\"";
                sb.AppendLine(line);
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var stream = new MemoryStream(bytes);
            return new FileStreamResult(stream, "text/csv") { FileDownloadName = "users.csv" };
        }

        public async Task<List<(Guid Id, string Name)>> GetRolesSimpleAsync()
        {
            var list = await _roleManager.Roles
        .AsNoTracking()
        .Select(r => new { r.Id, r.Name })
        .ToListAsync();

            // Convert to tuple after data is loaded into memory
            return list.Select(x => (x.Id, x.Name)).ToList();
        }

        public async Task<(bool Success, string? ErrorMessage)> ToggleStatusAsync(Guid id, string modifiedBy)
        {
            var u = await _userManager.FindByIdAsync(id.ToString());
            if (u == null)
                return (false, "User not found");

            u.IsActive = !u.IsActive;
            u.ModifiedOn = DateTime.UtcNow;
            u.ModifiedBy = modifiedBy;
            var result = await _userManager.UpdateAsync(u);
            if (!result.Succeeded)
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));

            return (true, null);
        }

        public async Task<(bool Success, string? Error, string? ErrorCode)> ChangePasswordAsync(Guid id, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return (false, "User not found", "NotFound");

            // Check current password
            var valid = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!valid)
                return (false, "The current password is incorrect.", "InvalidPassword");

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors, "ChangeFailed");
            }

            return (true, null, null);
        }




    }
}
