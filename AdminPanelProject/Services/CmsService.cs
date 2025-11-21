using AdminPanelProject.Data; // your DbContext namespace
using AdminPanelProject.Dtos.Cms;
using AdminPanelProject.Models;
using AdminPanelProject.ViewModels;
using AdminPanelProject.ViewModels.EmailTemplate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AdminPanelProject.Services
{
    public class CmsService : ICmsService
    {
        private readonly ApplicationDbContext _db;

        public CmsService(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<(IReadOnlyList<CmsDto> items, int total)> GetPagedAsync(
            int pageNumber, int pageSize,
            string? title, string? key, string? metaKeyword, bool? isActive, string sortField, string sortDirection)
        {
            var q = _db.CmsPages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                q = q.Where(x => x.Title.Contains(title));
            if (!string.IsNullOrWhiteSpace(key))
                q = q.Where(x => x.Key.Contains(key));
            if (!string.IsNullOrWhiteSpace(metaKeyword))
                q = q.Where(x => x.MetaKeyword.Contains(metaKeyword));
            if (isActive.HasValue)
                q = q.Where(x => x.IsActive == isActive.Value);

            // Sorting
            sortField = sortField?.Trim().ToLower();
            sortDirection = sortDirection?.Trim().ToLower();

            q = (sortField, sortDirection) switch
            {
                ("title", "desc") => q.OrderByDescending(r => r.Title),
                ("title", "asc") => q.OrderBy(r => r.Title),

                ("key", "desc") => q.OrderByDescending(r => r.Key),
                ("key", "asc") => q.OrderBy(r => r.Key),

                ("metakeyword", "desc") => q.OrderByDescending(r => r.MetaKeyword),
                ("metakeyword", "asc") => q.OrderBy(r => r.MetaKeyword),

                ("isactive", "desc") => q.OrderByDescending(r => r.IsActive),
                ("isactive", "asc") => q.OrderBy(r => r.IsActive),

                ("createdon", "desc") => q.OrderByDescending(r => r.CreatedAt),
                ("createdon", "asc") => q.OrderBy(r => r.CreatedAt),

                ("name", "desc") => q.OrderByDescending(r => r.CreatedBy),
                _ => q.OrderBy(r => r.CreatedBy) // default
            };


            var total = await q.CountAsync();

            // Calculate total pages
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // Adjust pageNumber if it exceeds totalPages
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = totalPages;
            }

            var items = await q
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CmsDto
                {
                    Id = x.Id,
                    Key = x.Key,
                    Title = x.Title,
                    MetaKeyword = x.MetaKeyword,
                    MetaTitle = x.MetaTitle,
                    MetaDescription = x.MetaDescription,
                    Content = x.Content,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedAt = x.ModifiedAt
                })
                .ToListAsync();

            return (items, total);
        }


        public async Task<CmsDto?> GetByIdAsync(Guid id)
        {
            var e = await _db.CmsPages.FindAsync(id);
            if (e == null) return null;
            return new CmsDto
            {
                Id = e.Id,
                Key = e.Key,
                Title = e.Title,
                MetaKeyword = e.MetaKeyword,
                MetaTitle = e.MetaTitle,
                MetaDescription = e.MetaDescription,
                Content = e.Content,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                CreatedBy = e.CreatedBy,
                ModifiedAt = e.ModifiedAt,
                ModifiedBy = e.ModifiedBy
            };
        }

        public async Task<(bool Success, string? Error, string? ErrorCode)> CreateAsync(CreateCmsDto dto, string createdBy)
        {
            //if (await _db.EmailTemplates.AnyAsync(x => x.Key == dto.Key))

            try
            {

            // Unique key check
            if (await _db.CmsPages.AnyAsync(x => x.Key == dto.Key))
                return (false, "An cms with this key already exists.", "Duplicate");
                

            var e = new CmsEntity
            {
                Id = Guid.NewGuid(),
                Key = dto.Key,
                Title = dto.Title,
                MetaKeyword = dto.MetaKeyword,
                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                Content = dto.Content,
                IsActive = dto.IsActive,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.CmsPages.Add(e);
            await _db.SaveChangesAsync();
            return (true, null, null);
            }
            catch (DbUpdateException ex)
            {
                //_logger.LogError(ex, "Database update error while creating EmailTemplate");
                return (false, "A database error occurred while creating the email template.", "DbError");
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Unexpected error in CreateAsync");
                return (false, "An unexpected error occurred while creating the email template.", "Unexpected");
            }
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateCmsDto dto, string modifiedBy)
        {
            var e = await _db.CmsPages.FindAsync(id);
            if (e == null) return false;

            // Key is not updated (as requested)
            e.Title = dto.Title;
            e.MetaKeyword = dto.MetaKeyword;
            e.MetaTitle = dto.MetaTitle;
            e.MetaDescription = dto.MetaDescription;
            e.Content = dto.Content;
            e.IsActive = dto.IsActive;
            e.ModifiedBy = modifiedBy;
            e.ModifiedAt = DateTime.UtcNow;

            _db.CmsPages.Update(e);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var e = await _db.CmsPages.FindAsync(id);
            if (e == null) return false;
            _db.CmsPages.Remove(e);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<FileContentResult> ExportCsvAsync(string? title, string? key, string? metaKeyword, bool? isActive)
        {
            var q = _db.CmsPages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(title)) q = q.Where(x => x.Title.Contains(title));
            if (!string.IsNullOrWhiteSpace(key)) q = q.Where(x => x.Key.Contains(key));
            if (!string.IsNullOrWhiteSpace(metaKeyword)) q = q.Where(x => x.MetaKeyword.Contains(metaKeyword));
            if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive.Value);

            var items = await q.OrderBy(x => x.Title).Select(x => new { x.Title, x.Key, MetaKeyword = x.MetaKeyword, Status = x.IsActive ? "Active" : "In Active" }).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Title,Key,MetaKeyword,Status");
            foreach (var it in items)
            {
                // Escape commas/quotes
                string safe(string s) =>
                    string.IsNullOrEmpty(s) ? "" : $"\"{s.Replace("\"", "\"\"")}\"";

                sb.AppendLine($"{safe(it.Title)},{safe(it.Key)},{safe(it.MetaKeyword)},{safe(it.Status)}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"cms-export-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return new FileContentResult(bytes, "text/csv") { FileDownloadName = fileName };
        }




        public async Task<bool> ToggleStatusAsync(Guid id, string modifiedBy)
        {
            var e = await _db.CmsPages.FindAsync(id);
            if (e == null) return false;

            e.IsActive = !e.IsActive;
            e.ModifiedBy = modifiedBy;
            e.ModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
