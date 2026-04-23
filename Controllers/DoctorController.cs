using EyeClinicApp.Data;
using EyeClinicApp.Models;
using EyeClinicApp.Services;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EyeClinicApp.Controllers
{
    [Authorize(Roles = AppRoles.Doctor)]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPrescriptionService _prescriptionService;

        public DoctorController(ApplicationDbContext context, IPrescriptionService prescriptionService)
        {
            _context = context;
            _prescriptionService = prescriptionService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            return RedirectToAction(nameof(MyAppointments));
        }

        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.TimeSlot)
                .Include(a => a.User)
                .Include(a => a.Prescription)
                .Where(a => a.AssignedDoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View("Dashboard", appointments);
        }

        [HttpGet]
        public async Task<IActionResult> AddPrescription(int appointmentId)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Prescription)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.AssignedDoctorId == doctorId);
            if (appointment is null)
            {
                return Forbid();
            }

            if (appointment.Prescription is not null)
            {
                TempData["Error"] = "Prescription already exists for this appointment.";
                return RedirectToAction(nameof(MyAppointments));
            }

            return View("UploadPrescription", new PrescriptionUploadViewModel { AppointmentId = appointmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrescription(PrescriptionUploadViewModel model)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View("UploadPrescription", model);
            }

            var canAccess = await _context.Appointments.AnyAsync(a => a.Id == model.AppointmentId && a.AssignedDoctorId == doctorId);
            if (!canAccess)
            {
                return Forbid();
            }

            var result = await _prescriptionService.UploadAsync(model, doctorId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Unable to upload prescription.");
                return View("UploadPrescription", model);
            }

            TempData["Success"] = "Prescription uploaded successfully.";
            return RedirectToAction(nameof(MyAppointments));
        }

        [HttpGet]
        public Task<IActionResult> UploadPrescription(int appointmentId) => AddPrescription(appointmentId);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> UploadPrescription(PrescriptionUploadViewModel model) => AddPrescription(model);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkComplete(int appointmentId)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Challenge();
            }

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment is null)
            {
                return NotFound();
            }

            if (appointment.AssignedDoctorId != doctorId)
            {
                return Forbid();
            }

            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment marked as completed.";
            return RedirectToAction(nameof(MyAppointments));
        }
    }
}
