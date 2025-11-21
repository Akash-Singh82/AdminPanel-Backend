

using AdminPanelProject.Data;
using AdminPanelProject.Models;
using AdminPanelProject.ViewModels;
using AdminPanelProject.ViewModels.EmailTemplate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminPanelProject.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(ApplicationDbContext db, ILogger<EmailTemplateService> logger)
        {
            _db = db;
            _logger = logger;
        }


        public async Task<PagedResult<EmailTemplateListItemDto>> GetPagedAsync(
                                                     int pageNumber,
                                                     int pageSize,
                                                     string? key,
                                                     string? title,
                                                     string? subject,
                                                     bool? isActive,
                                                     string? sortField,
                                                     string? sortDirection
                                                                   )
        {
            var query = _db.EmailTemplates.AsNoTracking().OrderByDescending(e => e.CreatedAt).AsQueryable();

            if (!string.IsNullOrWhiteSpace(key))
            {
                var s = key.Trim().ToLower();
                query = query.Where(e => e.Key.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                var s = title.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(subject))
            {
                var s = subject.Trim().ToLower();
                query = query.Where(e => e.Subject.ToLower().Contains(s));
            }

            if (isActive.HasValue)
            {
                query = query.Where(e => e.IsActive == isActive.Value);
            }

            // Sorting
            sortField = sortField?.Trim().ToLower();
            sortDirection = sortDirection?.Trim().ToLower();

            query = (sortField, sortDirection) switch
            {
                ("key", "desc") => query.OrderByDescending(r => r.Key),
                ("key", "asc") => query.OrderBy(r => r.Key),

                ("isactive", "desc") => query.OrderByDescending(r => r.IsActive),
                ("isactive", "asc") => query.OrderBy(r => r.IsActive),

                ("createdon", "desc") => query.OrderByDescending(r => r.CreatedAt),
                ("createdon", "asc") => query.OrderBy(r => r.CreatedAt),

                ("createdby", "desc") => query.OrderByDescending(r => r.CreatedBy),
                ("createdby", "asc") => query.OrderBy(r => r.CreatedBy),

                ("title", "desc") => query.OrderByDescending(r => r.Title),
                ("title", "asc") => query.OrderBy(r => r.Title),

                ("subject", "desc") => query.OrderByDescending(r => r.Subject),
                _ => query.OrderBy(r => r.Subject) // default
            };




            var total = await query.CountAsync();



            // Calculate total pages
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // Adjust pageNumber if it exceeds totalPages
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = totalPages;
            }



            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmailTemplateListItemDto(
                    e.Id,
                    e.Key,
                    e.Title,
                    e.Subject,
                    e.IsActive
                ))
                .ToListAsync();

            return new PagedResult<EmailTemplateListItemDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<EmailTemplateDetailsDto?> GetByIdAsync(Guid id)
        {
            var e = await _db.EmailTemplates.FindAsync(id);
            if (e == null)
                return null;

            return new EmailTemplateDetailsDto
            {
                Id = e.Id,
                Key = e.Key,
                Title = e.Title,
                Subject = e.Subject,
                FromEmail = e.FromEmail,
                FromName = e.FromName,
                IsActive = e.IsActive,
                IsManualMail = e.IsManualMail,
                Body = e.Body

            };

        }




        public async Task<(bool Success, string? Error, string? ErrorCode, EmailTemplateDetailsDto? Result)> CreateAsync(EmailTemplateCreateDto dto, string createdBy)
        {
            try
            {
                if (await _db.EmailTemplates.AnyAsync(x => x.Key == dto.Key))
                    return (false, "An email template with this key already exists.", "Duplicate", null);

                var entity = new EmailTemplate
                {
                    Id = Guid.NewGuid(),
                    Key = dto.Key,
                    Title = dto.Title,
                    Subject = dto.Subject,
                    FromEmail = dto.FromEmail,
                    FromName = dto.FromName,
                    IsActive = dto.IsActive,
                    IsManualMail = dto.IsManualMail,
                    Body = dto.Body,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _db.EmailTemplates.Add(entity);
                await _db.SaveChangesAsync();

                var result = new EmailTemplateDetailsDto
                {
                    Id = entity.Id,
                    Key = entity.Key,
                    Title = entity.Title,
                    Subject = entity.Subject,
                    FromEmail = entity.FromEmail,
                    FromName = entity.FromName,
                    IsActive = entity.IsActive,
                    IsManualMail = entity.IsManualMail,
                    Body = entity.Body
                };

                return (true, null, null, result);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error while creating EmailTemplate");
                return (false, "A database error occurred while creating the email template.", "DbError", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateAsync");
                return (false, "An unexpected error occurred while creating the email template.", "Unexpected", null);
            }
        }

        public async Task<(bool Success, string? Error, string? ErrorCode, EmailTemplateDetailsDto? Result)> UpdateAsync(EmailTemplateEditDto dto, string updatedBy)
        {
            try
            {
                var entity = await _db.EmailTemplates.FindAsync(dto.Id);
                if (entity == null)
                    return (false, "Email template not found.", "NotFound", null);

                if (!string.Equals(entity.Key, dto.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _db.EmailTemplates.AnyAsync(x => x.Key == dto.Key && x.Id != dto.Id))
                        return (false, "An email template with this key already exists.", "Duplicate", null);
                }

                entity.Key = dto.Key;
                entity.Title = dto.Title;
                entity.Subject = dto.Subject;
                entity.FromEmail = dto.FromEmail;
                entity.FromName = dto.FromName;
                entity.IsActive = dto.IsActive;
                entity.IsManualMail = dto.IsManualMail;
                entity.Body = dto.Body;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = updatedBy;

                _db.EmailTemplates.Update(entity);
                await _db.SaveChangesAsync();

                var result = new EmailTemplateDetailsDto
                {
                    Id = entity.Id,
                    Key = entity.Key,
                    Title = entity.Title,
                    Subject = entity.Subject,
                    FromEmail = entity.FromEmail,
                    FromName = entity.FromName,
                    IsActive = entity.IsActive,
                    IsManualMail = entity.IsManualMail,
                    Body = entity.Body
                };

                return (true, null, null, result);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error while updating EmailTemplate");
                return (false, "A database error occurred while updating the email template.", "DbError", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateAsync");
                return (false, "An unexpected error occurred while updating the email template.", "Unexpected", null);
            }
        }

        public async Task<(bool Success, string? Error, string? ErrorCode)> DeleteAsync(Guid id)
        {
            try
            {
                var e = await _db.EmailTemplates.FindAsync(id);
                if (e == null)
                    return (false, "Email template not found.", "NotFound");

                _db.EmailTemplates.Remove(e);
                await _db.SaveChangesAsync();
                return (true, null, null);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error while deleting EmailTemplate");
                return (false, "A database error occurred while deleting the email template.", "DbError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteAsync");
                return (false, "An unexpected error occurred while deleting the email template.", "Unexpected");
            }
        }

        public async Task<(bool Success, string? message)> ToggleStatusAsync(Guid id, string updatedBy)
        {
            var e = await _db.EmailTemplates.FindAsync(id);
            if (e == null) return (false, "Email Template not found" );
            

            e.IsActive = !e.IsActive;
            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return (true, null);
        }



        public async Task<FileContentResult> ExportCsvAsync(string? key, string? title, string? subject, bool? isActive)
        {
            var q = _db.EmailTemplates.AsQueryable();
            if (!string.IsNullOrWhiteSpace(key)) q = q.Where(x => x.Key.Contains(key));
            if (!string.IsNullOrWhiteSpace(title)) q = q.Where(x => x.Title.Contains(title));
            if (!string.IsNullOrWhiteSpace(subject)) q = q.Where(x => x.Subject.Contains(subject));
            if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive.Value);

            var items = await q.OrderBy(x => x.Title).Select(x => new
            {
                x.Key,
                x.Title,
                x.Subject,
                Status = x.IsActive ? "Active" : "Inactive"
            }).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Key,Title,Subject,Status");
            foreach (var it in items)
            {
                string Safe(string s) => string.IsNullOrEmpty(s) ? "" : $"\"{s.Replace("\"", "\"\"")}\"";
                sb.AppendLine($"{Safe(it.Key)},{Safe(it.Title)},{Safe(it.Subject)},{Safe(it.Status)}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"emailtemplates-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return new FileContentResult(bytes, "text/csv") { FileDownloadName = fileName };
        }


    }


}

