using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Range(0, 1000000)]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = OrderStatus.Pending;

        [Required]
        [MaxLength(40)]
        public string PaymentMethod { get; set; } = global::EyeClinicApp.Models.PaymentMethod.CashOnDelivery;

        [Required]
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = OrderPaymentStatus.Pending;

        [MaxLength(150)]
        public string? PaymentId { get; set; }

        [MaxLength(150)]
        public string? RazorpayOrderId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
