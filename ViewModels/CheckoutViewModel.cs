using System.ComponentModel.DataAnnotations;
using EyeClinicApp.Models;

namespace EyeClinicApp.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a payment method.")]
        public string PaymentMethod { get; set; } = string.Empty;

        public bool IsUpiPaymentAcknowledged { get; set; }

        public string? RazorpayOrderId { get; set; }
        public int? PendingOrderId { get; set; }
        public string RazorpayKeyId { get; set; } = string.Empty;
        public string UpiQrImageUrl { get; set; } = string.Empty;
        public string UpiId { get; set; } = string.Empty;

        public List<CartRowViewModel> Items { get; set; } = [];
        public decimal Total => Items.Sum(i => i.LineTotal);
    }
}
