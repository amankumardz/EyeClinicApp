using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace EyeClinicApp.Services
{
    public class AppointmentPaymentService : IAppointmentPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AppointmentPaymentService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool IsSuccess, string? Error, object? Payload)> CreateOnlinePaymentOrderAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            var keyId = _configuration["Razorpay:KeyId"];
            var keySecret = _configuration["Razorpay:KeySecret"];
            if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(keySecret))
            {
                return (false, "Razorpay is not configured.", null);
            }

            var client = new RazorpayClient(keyId, keySecret);
            var options = new Dictionary<string, object>
            {
                { "amount", 50000 }, // ₹500 consultation fee
                { "currency", "INR" },
                { "receipt", $"appt_{appointment.Id}" }
            };

            var razorpayOrder = client.Order.Create(options);
            appointment.RazorpayOrderId = razorpayOrder["id"]?.ToString();
            appointment.PaymentStatus = AppointmentPaymentStatus.Pending;

            await _context.SaveChangesAsync(cancellationToken);

            return (true, null, new
            {
                key = keyId,
                amount = options["amount"],
                currency = options["currency"],
                razorpayOrderId = appointment.RazorpayOrderId,
                appointmentId = appointment.Id,
                customer = new
                {
                    name = appointment.Name,
                    email = appointment.Email,
                    contact = appointment.PhoneNumber
                }
            });
        }

        public async Task<(bool IsSuccess, string? Error)> VerifyOnlinePaymentAsync(Appointment appointment, string razorpayOrderId, string razorpayPaymentId, string razorpaySignature, CancellationToken cancellationToken = default)
        {
            var keySecret = _configuration["Razorpay:KeySecret"] ?? string.Empty;
            if (!VerifyRazorpaySignature(razorpayOrderId, razorpayPaymentId, razorpaySignature, keySecret))
            {
                appointment.PaymentStatus = AppointmentPaymentStatus.Failed;
                await _context.SaveChangesAsync(cancellationToken);
                return (false, "Payment verification failed.");
            }

            appointment.PaymentStatus = AppointmentPaymentStatus.Paid;
            appointment.RazorpayPaymentId = razorpayPaymentId;
            appointment.RazorpayOrderId = razorpayOrderId;

            await _context.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        private static bool VerifyRazorpaySignature(string orderId, string paymentId, string signature, string keySecret)
        {
            if (string.IsNullOrWhiteSpace(keySecret))
            {
                return false;
            }

            var payload = $"{orderId}|{paymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var generatedSignature = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            return string.Equals(generatedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }
    }
}
