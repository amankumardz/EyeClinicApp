namespace EyeClinicApp.Models
{
    public static class AppointmentPaymentMethod
    {
        public const string Online = "Online";
        public const string Clinic = "Clinic";

        public static readonly string[] All = [Online, Clinic];
    }
}
