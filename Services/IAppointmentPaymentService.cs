using EyeClinicApp.Models;

namespace EyeClinicApp.Services
{
    public interface IAppointmentPaymentService
    {
        Task<(bool IsSuccess, string? Error, object? Payload)> CreateOnlinePaymentOrderAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task<(bool IsSuccess, string? Error)> VerifyOnlinePaymentAsync(Appointment appointment, string razorpayOrderId, string razorpayPaymentId, string razorpaySignature, CancellationToken cancellationToken = default);
    }
}
