namespace EyeClinicApp.Models
{
    public static class PaymentMethod
    {
        public const string UpiQr = "UPI_QR";
        public const string Razorpay = "RAZORPAY";
        public const string CashOnDelivery = "COD";

        public static readonly string[] All = [UpiQr, Razorpay, CashOnDelivery];
    }
}
