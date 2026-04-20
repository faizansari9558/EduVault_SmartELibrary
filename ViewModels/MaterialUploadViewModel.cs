using System.ComponentModel.DataAnnotations;
using SmartELibrary.Models;

namespace SmartELibrary.ViewModels;

public class MaterialUploadViewModel
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public MaterialType MaterialType { get; set; }

    [Required]
    public int SemesterId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    public int? TopicId { get; set; }

    public bool IsPublic { get; set; }

    public IFormFile? File { get; set; }

    [Url]
    public string? ExternalUrl { get; set; }
}
