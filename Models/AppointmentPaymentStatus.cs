namespace EyeClinicApp.Models
{
    public static class AppointmentPaymentStatus
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
        public const string Failed = "Failed";

        public static readonly string[] All = [Pending, Paid, Failed];
    }
}
