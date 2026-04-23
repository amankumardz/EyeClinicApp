using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.ViewModels
{
    public class PrescriptionUploadViewModel
    {
        [Required]
        public int AppointmentId { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [Display(Name = "Prescription File")]
        public IFormFile? File { get; set; }

        [Display(Name = "Right Eye SPH")]
        [MaxLength(120)]
        public string? RightEyeSph { get; set; }

        [Display(Name = "Right Eye CYL")]
        [MaxLength(120)]
        public string? RightEyeCyl { get; set; }

        [Display(Name = "Right Eye Axis")]
        [MaxLength(120)]
        public string? RightEyeAxis { get; set; }

        [Display(Name = "Left Eye SPH")]
        [MaxLength(120)]
        public string? LeftEyeSph { get; set; }

        [Display(Name = "Left Eye CYL")]
        [MaxLength(120)]
        public string? LeftEyeCyl { get; set; }

        [Display(Name = "Left Eye Axis")]
        [MaxLength(120)]
        public string? LeftEyeAxis { get; set; }
    }

    public class AssignDoctorViewModel
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string TimeSlotLabel { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Doctor")]
        public string SelectedDoctorId { get; set; } = string.Empty;

        public IReadOnlyCollection<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> DoctorOptions { get; set; }
            = Array.Empty<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    }

    public class AppointmentDetailsViewModel
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public bool HasPrescription { get; set; }
        public PrescriptionSummaryViewModel? Prescription { get; set; }
    }

    public class PrescriptionSummaryViewModel
    {
        public int Id { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RightEyeSph { get; set; }
        public string? RightEyeCyl { get; set; }
        public string? RightEyeAxis { get; set; }
        public string? LeftEyeSph { get; set; }
        public string? LeftEyeCyl { get; set; }
        public string? LeftEyeAxis { get; set; }
    }
}
