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

        [Required]
        [MaxLength(20)]
        public string Shift { get; set; } = TimeSlotShift.Morning;

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? Label { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        public string GetDisplayLabel() => !string.IsNullOrWhiteSpace(Label)
            ? Label
            : $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

        public static string ResolveShift(TimeSpan startTime) => startTime switch
        {
            var t when t >= new TimeSpan(9, 0, 0) && t < new TimeSpan(12, 0, 0) => TimeSlotShift.Morning,
            var t when t >= new TimeSpan(12, 0, 0) && t < new TimeSpan(16, 0, 0) => TimeSlotShift.Afternoon,
            _ => TimeSlotShift.Evening
        };
    }

    public static class TimeSlotShift
    {
        public const string Morning = "Morning";
        public const string Afternoon = "Afternoon";
        public const string Evening = "Evening";

        public static readonly string[] Ordered = [Morning, Afternoon, Evening];
    }
}
