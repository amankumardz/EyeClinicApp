using EyeClinicApp.Data;
using EyeClinicApp.Models;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EyeClinicApp.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/png",
            "image/jpeg",
            "image/jpg"
        };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PrescriptionService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<(bool IsSuccess, string? Error, Prescription? Prescription)> UploadAsync(PrescriptionUploadViewModel model, string doctorId, CancellationToken cancellationToken = default)
        {
            var hasStructuredValues = !string.IsNullOrWhiteSpace(model.Notes)
                || !string.IsNullOrWhiteSpace(model.RightEyeSph)
                || !string.IsNullOrWhiteSpace(model.RightEyeCyl)
                || !string.IsNullOrWhiteSpace(model.RightEyeAxis)
                || !string.IsNullOrWhiteSpace(model.LeftEyeSph)
                || !string.IsNullOrWhiteSpace(model.LeftEyeCyl)
                || !string.IsNullOrWhiteSpace(model.LeftEyeAxis);

            if (model.File is null && !hasStructuredValues)
            {
                return (false, "Provide either prescription details or upload a file.", null);
            }

            var appointment = await _context.Appointments
                .Include(a => a.Prescription)
                .FirstOrDefaultAsync(a => a.Id == model.AppointmentId, cancellationToken);

            if (appointment is null)
            {
                return (false, "Appointment not found.", null);
            }

            if (appointment.Prescription is not null)
            {
                return (false, "Prescription already uploaded for this appointment.", null);
            }

            if (appointment.AssignedDoctorId != doctorId)
            {
                return (false, "Only the assigned doctor can upload a prescription.", null);
            }

            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "prescriptions");
            Directory.CreateDirectory(uploadsRoot);
            string fileContentType;
            string safeFileName;

            if (model.File is not null)
            {
                if (model.File.Length == 0)
                {
                    return (false, "Uploaded file is empty.", null);
                }

                if (model.File.Length > MaxFileSizeBytes)
                {
                    return (false, "Maximum upload size is 5 MB.", null);
                }

                if (!AllowedTypes.Contains(model.File.ContentType))
                {
                    return (false, "Only PDF, PNG, and JPEG files are allowed.", null);
                }

                var extension = Path.GetExtension(model.File.FileName);
                safeFileName = $"prescription_{appointment.Id}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
                var fullPath = Path.Combine(uploadsRoot, safeFileName);

                await using (var fileStream = new FileStream(fullPath, FileMode.CreateNew))
                {
                    await model.File.CopyToAsync(fileStream, cancellationToken);
                }

                fileContentType = model.File.ContentType;
            }
            else
            {
                safeFileName = $"prescription_{appointment.Id}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.pdf";
                var fullPath = Path.Combine(uploadsRoot, safeFileName);
                var generatedPdf = BuildPrescriptionPdfBytes(appointment, model);
                await File.WriteAllBytesAsync(fullPath, generatedPdf, cancellationToken);
                fileContentType = "application/pdf";
            }

            var prescription = new Prescription
            {
                AppointmentId = appointment.Id,
                DoctorId = doctorId,
                Notes = model.Notes?.Trim(),
                FilePath = Path.Combine("uploads", "prescriptions", safeFileName).Replace("\\", "/"),
                FileContentType = fileContentType,
                RightEyeSph = model.RightEyeSph,
                RightEyeCyl = model.RightEyeCyl,
                RightEyeAxis = model.RightEyeAxis,
                LeftEyeSph = model.LeftEyeSph,
                LeftEyeCyl = model.LeftEyeCyl,
                LeftEyeAxis = model.LeftEyeAxis,
                CreatedAt = DateTime.UtcNow
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync(cancellationToken);

            return (true, null, prescription);
        }

        private static byte[] BuildPrescriptionPdfBytes(Appointment appointment, PrescriptionUploadViewModel model)
        {
            var content = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Content().Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Text("Patient Prescription").FontSize(18).Bold();
                        col.Item().Text($"Patient: {appointment.Name}");
                        col.Item().Text($"Appointment Date: {appointment.AppointmentDate:dd MMM yyyy}");
                        col.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC");
                        col.Item().PaddingTop(8).Text($"Notes: {model.Notes ?? "N/A"}");
                        col.Item().Text($"OD SPH/CYL/Axis: {model.RightEyeSph ?? "-"} / {model.RightEyeCyl ?? "-"} / {model.RightEyeAxis ?? "-"}");
                        col.Item().Text($"OS SPH/CYL/Axis: {model.LeftEyeSph ?? "-"} / {model.LeftEyeCyl ?? "-"} / {model.LeftEyeAxis ?? "-"}");
                    });
                });
            });

            return content.GeneratePdf();
        }

        public async Task<AppointmentDetailsViewModel?> GetAppointmentDetailsForPatientAsync(int appointmentId, string patientUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.TimeSlot)
                .Include(a => a.Prescription)
                .Where(a => a.Id == appointmentId && a.UserId == patientUserId)
                .Select(a => new AppointmentDetailsViewModel
                {
                    AppointmentId = a.Id,
                    PatientName = a.Name,
                    AppointmentDate = a.AppointmentDate,
                    TimeSlot = a.TimeSlot != null ? a.TimeSlot.Label : "N/A",
                    Status = a.Status,
                    PaymentMethod = a.PaymentMethod,
                    PaymentStatus = a.PaymentStatus,
                    HasPrescription = a.Prescription != null,
                    Prescription = a.Prescription == null ? null : new PrescriptionSummaryViewModel
                    {
                        Id = a.Prescription.Id,
                        Notes = a.Prescription.Notes,
                        CreatedAt = a.Prescription.CreatedAt,
                        RightEyeSph = a.Prescription.RightEyeSph,
                        RightEyeCyl = a.Prescription.RightEyeCyl,
                        RightEyeAxis = a.Prescription.RightEyeAxis,
                        LeftEyeSph = a.Prescription.LeftEyeSph,
                        LeftEyeCyl = a.Prescription.LeftEyeCyl,
                        LeftEyeAxis = a.Prescription.LeftEyeAxis
                    }
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<(byte[] Content, string ContentType, string FileName)?> GetPrescriptionDownloadAsync(int appointmentId, string patientUserId, CancellationToken cancellationToken = default)
        {
            var data = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Prescription)
                .Where(a => a.Id == appointmentId && a.UserId == patientUserId && a.Prescription != null)
                .Select(a => new
                {
                    a.Name,
                    a.AppointmentDate,
                    Prescription = a.Prescription!
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (data is null)
            {
                return null;
            }

            var sourcePath = Path.Combine(_environment.WebRootPath, data.Prescription.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (!File.Exists(sourcePath))
            {
                return null;
            }

            if (string.Equals(data.Prescription.FileContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return (await File.ReadAllBytesAsync(sourcePath, cancellationToken), "application/pdf", $"prescription-{appointmentId}.pdf");
            }

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Patient Prescription").FontSize(18).Bold();
                        col.Item().Text($"Patient: {data.Name}");
                        col.Item().Text($"Appointment Date: {data.AppointmentDate:dd MMM yyyy}");
                        col.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC");
                        col.Item().PaddingVertical(10).Text(data.Prescription.Notes ?? "No notes provided.");
                        col.Item().Text($"OD SPH/CYL/Axis: {data.Prescription.RightEyeSph} / {data.Prescription.RightEyeCyl} / {data.Prescription.RightEyeAxis}");
                        col.Item().Text($"OS SPH/CYL/Axis: {data.Prescription.LeftEyeSph} / {data.Prescription.LeftEyeCyl} / {data.Prescription.LeftEyeAxis}");
                    });
                });
            }).GeneratePdf();

            return (pdfBytes, "application/pdf", $"prescription-{appointmentId}.pdf");
        }

        public async Task<Prescription?> GetLatestForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Prescriptions
                .AsNoTracking()
                .Include(p => p.Appointment)
                .Where(p => p.Appointment != null && p.Appointment.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
