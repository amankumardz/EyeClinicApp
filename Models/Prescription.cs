using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [Required]
        [MaxLength(450)]
        public string DoctorId { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? FileContentType { get; set; }

        [MaxLength(120)]
        public string? RightEyeSph { get; set; }

        [MaxLength(120)]
        public string? RightEyeCyl { get; set; }

        [MaxLength(120)]
        public string? RightEyeAxis { get; set; }

        [MaxLength(120)]
        public string? LeftEyeSph { get; set; }

        [MaxLength(120)]
        public string? LeftEyeCyl { get; set; }

        [MaxLength(120)]
        public string? LeftEyeAxis { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Appointment? Appointment { get; set; }
        public ApplicationUser? Doctor { get; set; }
    }
}
