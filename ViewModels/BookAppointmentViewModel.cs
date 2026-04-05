using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.ViewModels
{
    public class BookAppointmentViewModel
    {
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

        [Display(Name = "Reason")]
        [MaxLength(1000)]
        public string? ReasonForVisit { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public int TimeSlotId { get; set; }

        public string SelectedTimeSlotLabel { get; set; } = string.Empty;
    }

    public class BookAppointmentSlotSelectionViewModel
    {
        [DataType(DataType.Date)]
        public DateTime SelectedDate { get; set; }

        public int? SelectedSlotId { get; set; }

        public IReadOnlyCollection<DateTabViewModel> DateTabs { get; set; } = [];

        public IReadOnlyCollection<ShiftSlotGroupViewModel> ShiftGroups { get; set; } = [];
    }

    public class DateTabViewModel
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsDisabled { get; set; }
    }

    public class ShiftSlotGroupViewModel
    {
        public string Shift { get; set; } = string.Empty;
        public IReadOnlyCollection<SlotItemViewModel> Slots { get; set; } = [];
    }

    public class SlotItemViewModel
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsBooked { get; set; }
        public bool IsExpired { get; set; }
    }
}
