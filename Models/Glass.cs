using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class Glass
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Url, StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
    }
}
