using EyeClinicApp.Models;
using EyeClinicApp.ViewModels;

namespace EyeClinicApp.Services
{
    public interface IPrescriptionService
    {
        Task<(bool IsSuccess, string? Error, Prescription? Prescription)> UploadAsync(PrescriptionUploadViewModel model, string doctorId, CancellationToken cancellationToken = default);
        Task<AppointmentDetailsViewModel?> GetAppointmentDetailsForPatientAsync(int appointmentId, string patientUserId, CancellationToken cancellationToken = default);
        Task<(byte[] Content, string ContentType, string FileName)?> GetPrescriptionDownloadAsync(int appointmentId, string patientUserId, CancellationToken cancellationToken = default);
        Task<Prescription?> GetLatestForUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}
