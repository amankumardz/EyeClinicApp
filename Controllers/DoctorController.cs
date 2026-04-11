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
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.TimeSlot)
                .Include(a => a.Prescription)
                .Where(a => a.AssignedDoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        [HttpGet]
        public async Task<IActionResult> UploadPrescription(int appointmentId)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canAccess = await _context.Appointments.AnyAsync(a => a.Id == appointmentId && a.AssignedDoctorId == doctorId);
            if (!canAccess)
            {
                return Forbid();
            }

            return View(new PrescriptionUploadViewModel { AppointmentId = appointmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPrescription(PrescriptionUploadViewModel model)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
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
                return View(model);
            }

            TempData["Success"] = "Prescription uploaded successfully.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
