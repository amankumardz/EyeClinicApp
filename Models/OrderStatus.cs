namespace EyeClinicApp.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";

        public static readonly string[] Flow = [Pending, Confirmed, Shipped, Delivered];
    }
}
