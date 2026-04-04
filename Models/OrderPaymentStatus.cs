namespace EyeClinicApp.Models
{
    public static class OrderPaymentStatus
    {
        public const string Pending = "Pending";
        public const string AwaitingConfirmation = "AwaitingConfirmation";
        public const string Paid = "Paid";
        public const string Failed = "Failed";

        public static readonly string[] All = [Pending, AwaitingConfirmation, Paid, Failed];
    }
}
