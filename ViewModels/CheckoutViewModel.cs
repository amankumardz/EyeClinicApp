using System.ComponentModel.DataAnnotations;

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

        public List<CartRowViewModel> Items { get; set; } = [];
        public decimal Total => Items.Sum(i => i.LineTotal);
    }
}
