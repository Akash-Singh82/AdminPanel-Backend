using System;
using System.ComponentModel.DataAnnotations;

namespace AdminPanelProject.Models
{   public class CmsPage
    {

         
    
    public Guid Id { get; set; } = Guid.NewGuid();


    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;


    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;


    [MaxLength(500)]
    public string MetaKeyword { get; set; } =string.Empty;


        public string Content { get; set; } = string.Empty;


    public bool IsActive { get; set; } = true;


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}


        }