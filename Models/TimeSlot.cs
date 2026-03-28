using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class TimeSlot
    {
        public int Id { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? Label { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        public string GetDisplayLabel() => !string.IsNullOrWhiteSpace(Label)
            ? Label
            : $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}
