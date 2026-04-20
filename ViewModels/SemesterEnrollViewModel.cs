using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class SemesterEnrollViewModel
{
    [Required]
    public int SemesterId { get; set; }
}
