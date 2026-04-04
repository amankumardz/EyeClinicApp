using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        public Order? Order { get; set; }

        [Required]
        public int GlassId { get; set; }

        public Glass? Glass { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        [Range(0, 100000)]
        public decimal Price { get; set; }
    }
}
