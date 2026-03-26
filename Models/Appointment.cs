namespace EyeClinicApp.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; }

        public ApplicationUser User { get; set; }
    }
}
