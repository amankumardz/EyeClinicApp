using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string ClientName { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [MaxLength(2000)]
        public string ReviewText { get; set; } = string.Empty;

        public string? ClientImageBase64 { get; set; }

        public bool IsApproved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
