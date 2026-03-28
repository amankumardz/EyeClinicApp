using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.ViewModels
{
    public class BookAppointmentViewModel
    {
        [DataType(DataType.DateTime)]
        [Display(Name = "Appointment Date")]
        [Required]
        public DateTime AppointmentDate { get; set; } = DateTime.UtcNow.AddDays(1);
    }
}
