namespace EyeClinicApp.Models
{
    public static class AppointmentStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Modified = "Modified";
        public const string Completed = "Completed";

        public static readonly string[] ActiveStatuses = [Pending, Approved, Modified];
    }
}
