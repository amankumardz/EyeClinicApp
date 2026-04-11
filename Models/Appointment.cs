using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(25)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(25)]
        public string NormalizedPhoneNumber { get; set; } = string.Empty;

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [Range(0, 130)]
        public int? Age { get; set; }

        [MaxLength(1000)]
        public string? ReasonForVisit { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public int TimeSlotId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = AppointmentStatus.Pending;

        [Required]
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = AppointmentPaymentStatus.Pending;

        [Required]
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = AppointmentPaymentMethod.Clinic;

        [MaxLength(120)]
        public string? RazorpayPaymentId { get; set; }

        [MaxLength(120)]
        public string? RazorpayOrderId { get; set; }

        [MaxLength(450)]
        public string? AssignedDoctorId { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [MaxLength(450)]
        public string? ModifiedByAdminId { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public TimeSlot? TimeSlot { get; set; }
        public ApplicationUser? ModifiedByAdmin { get; set; }
        public ApplicationUser? User { get; set; }
        public ApplicationUser? AssignedDoctor { get; set; }
        public Prescription? Prescription { get; set; }
    }
}
