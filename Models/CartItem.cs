using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public int GlassId { get; set; }

        public Glass? Glass { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
