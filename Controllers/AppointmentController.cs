using EyeClinicApp.Data;
using EyeClinicApp.Models;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace EyeClinicApp.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Book(DateTime? date)
        {
            var targetDate = (date ?? DateTime.UtcNow.Date.AddDays(1)).Date;
            if (targetDate <= DateTime.UtcNow.Date)
            {
                targetDate = DateTime.UtcNow.Date.AddDays(1);
            }

            var model = new BookAppointmentViewModel
            {
                AppointmentDate = targetDate,
                AvailableSlots = await GetAvailableSlotsAsync(targetDate)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookAppointmentViewModel model)
        {
            model.AppointmentDate = model.AppointmentDate.Date;
            model.AvailableSlots = await GetAvailableSlotsAsync(model.AppointmentDate);

            if (model.AppointmentDate <= DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(BookAppointmentViewModel.AppointmentDate), "Appointment must be scheduled at least one day in advance.");
            }

            if (!model.TimeSlotId.HasValue)
            {
                ModelState.AddModelError(nameof(BookAppointmentViewModel.TimeSlotId), "Please select an available slot.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedPhone = NormalizePhone(model.PhoneNumber);
            var nowDate = DateTime.UtcNow.Date;

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var hasSameDate = await _context.Appointments.AnyAsync(a =>
                a.NormalizedPhoneNumber == normalizedPhone &&
                a.AppointmentDate == model.AppointmentDate);

            if (hasSameDate)
            {
                ModelState.AddModelError(string.Empty, "You already have an appointment for the selected date.");
                await tx.RollbackAsync();
                return View(model);
            }

            var hasActiveBooking = await _context.Appointments.AnyAsync(a =>
                a.NormalizedPhoneNumber == normalizedPhone &&
                AppointmentStatus.ActiveStatuses.Contains(a.Status) &&
                a.AppointmentDate >= nowDate);

            if (hasActiveBooking)
            {
                ModelState.AddModelError(string.Empty, "You already have a pending/approved/modified appointment. Complete or reject it before booking a new one.");
                await tx.RollbackAsync();
                return View(model);
            }

            var appointment = new Appointment
            {
                Name = model.Name.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                NormalizedPhoneNumber = normalizedPhone,
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                Age = model.Age,
                ReasonForVisit = string.IsNullOrWhiteSpace(model.ReasonForVisit) ? null : model.ReasonForVisit.Trim(),
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                AppointmentDate = model.AppointmentDate,
                TimeSlotId = model.TimeSlotId!.Value,
                Status = AppointmentStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);

            try
            {
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(nameof(BookAppointmentViewModel.TimeSlotId), "That slot was just booked by another client. Please pick a different slot.");
                model.AvailableSlots = await GetAvailableSlotsAsync(model.AppointmentDate);
                return View(model);
            }

            TempData["Success"] = "Appointment submitted successfully."
                + " Status: Pending.";

            return RedirectToAction(nameof(Book), new { date = model.AppointmentDate });
        }

        private async Task<IReadOnlyCollection<SelectListItem>> GetAvailableSlotsAsync(DateTime date)
        {
            var activeSlotIds = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentDate == date && AppointmentStatus.ActiveStatuses.Contains(a.Status))
                .Select(a => a.TimeSlotId)
                .ToListAsync();

            var slots = await _context.TimeSlots
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.StartTime)
                .ToListAsync();

            return slots
                .Where(s => !activeSlotIds.Contains(s.Id))
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.GetDisplayLabel()
                })
                .ToList();
        }

        private static string NormalizePhone(string phone)
        {
            var buffer = new StringBuilder();
            foreach (var c in phone)
            {
                if (char.IsDigit(c))
                {
                    buffer.Append(c);
                }
            }

            return buffer.ToString();
        }
    }
}
