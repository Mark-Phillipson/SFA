using System;

namespace SFA_PWA.Models
{
    public class CustomLink
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    [System.ComponentModel.DataAnnotations.Required]
    public string? Name { get; set; }
    public string? Description { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Url]
    public string? Url { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}
