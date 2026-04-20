using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class MaterialPage
{
    public int Id { get; set; }

    public int MaterialId { get; set; }
    public Material? Material { get; set; }

    public int PageNumber { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string HtmlContent { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<MaterialPageProgress> PageProgress { get; set; } = new List<MaterialPageProgress>();
}
