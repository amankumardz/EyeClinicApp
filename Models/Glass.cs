using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class Glass
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Brand { get; set; } = string.Empty;

        [Range(0, 100000)]
        public decimal Price { get; set; }

        [Url]
        [MaxLength(1000)]
        public string? ImageUrl { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
