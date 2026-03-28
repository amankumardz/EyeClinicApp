using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }
    }
}
