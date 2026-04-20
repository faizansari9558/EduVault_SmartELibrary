using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class OtpVerification
{
    public int Id { get; set; }

    [Required, StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(6)]
    public string OtpCode { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
