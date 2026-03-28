using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required, StringLength(30)]
        public string Status { get; set; } = "Pending";

        public ApplicationUser? User { get; set; }
    }
}
