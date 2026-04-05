using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class UserOtp
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(6)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Purpose { get; set; } = string.Empty;

        public DateTime ExpiryTime { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAtUtc { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
