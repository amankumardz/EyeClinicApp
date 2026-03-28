using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.ViewModels
{
    public class AdminUpdateAppointmentViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\+?[0-9\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Range(0, 130)]
        public int? Age { get; set; }

        [Display(Name = "Reason for Visit")]
        [MaxLength(1000)]
        public string? ReasonForVisit { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public int? TimeSlotId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public IReadOnlyCollection<SelectListItem> AvailableSlots { get; set; } = [];
    }
}
