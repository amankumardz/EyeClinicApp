using EyeClinicApp.Data;
using EyeClinicApp.Helpers;
using EyeClinicApp.Models;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EyeClinicApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalGlasses = await _context.Glasses.CountAsync();
            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
            ViewBag.PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending);
            ViewBag.ApprovedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Approved);
            ViewBag.TotalTeamMembers = await _context.PersonProfiles.CountAsync();
            ViewBag.ApprovedReviews = await _context.Reviews.CountAsync(r => r.IsApproved);
            return View();
        }

        public async Task<IActionResult> ManageGlasses()
        {
            var glasses = await _context.Glasses.AsNoTracking().OrderBy(g => g.Brand).ThenBy(g => g.Name).ToListAsync();
            return View(glasses);
        }

        public IActionResult AddGlass() => View(new Glass());

        [HttpGet]
        public async Task<IActionResult> EditGlass(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var glass = await _context.Glasses.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
            if (glass is null)
            {
                return NotFound();
            }

            return View(glass);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGlass(Glass model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var glass = await _context.Glasses.FirstOrDefaultAsync(g => g.Id == model.Id);
            if (glass is null)
            {
                return NotFound();
            }

            if (!ImageUploadHelper.IsValidImageFile(imageFile, out var fileValidationError))
            {
                ModelState.AddModelError("imageFile", fileValidationError);
                return View(model);
            }

            glass.Name = model.Name.Trim();
            glass.Brand = model.Brand.Trim();
            glass.Price = model.Price;
            glass.Description = model.Description?.Trim();

            if (imageFile is not null)
            {
                glass.ImageBase64 = await ImageUploadHelper.ConvertToBase64Async(imageFile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageGlasses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGlass(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var glass = await _context.Glasses.FirstOrDefaultAsync(g => g.Id == id);
            if (glass is null)
            {
                return NotFound();
            }

            _context.Glasses.Remove(glass);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageGlasses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGlass(Glass glass, IFormFile? imageFile)
        {
            if (!ImageUploadHelper.IsValidImageFile(imageFile, out var fileValidationError))
            {
                ModelState.AddModelError("imageFile", fileValidationError);
            }

            if (!ModelState.IsValid)
            {
                return View(glass);
            }

            glass.ImageBase64 = await ImageUploadHelper.ConvertToBase64Async(imageFile);

            _context.Glasses.Add(glass);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageGlasses));
        }

        public async Task<IActionResult> ManageAppointments()
        {
            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.TimeSlot)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot!.StartTime)
                .ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var allowed = new[] { AppointmentStatus.Approved, AppointmentStatus.Rejected, AppointmentStatus.Completed };
            if (!allowed.Contains(status))
            {
                return BadRequest();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment is null)
            {
                return NotFound();
            }

            appointment.Status = status;
            appointment.UpdatedAtUtc = DateTime.UtcNow;
            appointment.ModifiedByAdminId = _userManager.GetUserId(User);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageAppointments));
        }

        [HttpGet]
        public async Task<IActionResult> EditAppointment(int id)
        {
            var appointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (appointment is null)
            {
                return NotFound();
            }

            var model = new AdminUpdateAppointmentViewModel
            {
                Id = appointment.Id,
                Name = appointment.Name,
                PhoneNumber = appointment.PhoneNumber,
                Email = appointment.Email,
                Age = appointment.Age,
                ReasonForVisit = appointment.ReasonForVisit,
                Address = appointment.Address,
                AppointmentDate = appointment.AppointmentDate,
                TimeSlotId = appointment.TimeSlotId,
                Status = appointment.Status,
                RowVersion = appointment.RowVersion,
                AvailableSlots = await GetSlotOptionsAsync(appointment.AppointmentDate, appointment.Id)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAppointment(AdminUpdateAppointmentViewModel model)
        {
            model.AppointmentDate = model.AppointmentDate.Date;
            model.AvailableSlots = await GetSlotOptionsAsync(model.AppointmentDate, model.Id);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == model.Id);
            if (appointment is null)
            {
                return NotFound();
            }

            var normalizedPhone = NormalizePhone(model.PhoneNumber);
            var hasConflictForClient = await _context.Appointments.AnyAsync(a =>
                a.Id != appointment.Id &&
                a.NormalizedPhoneNumber == normalizedPhone &&
                (a.AppointmentDate == model.AppointmentDate ||
                    (AppointmentStatus.ActiveStatuses.Contains(a.Status) && a.AppointmentDate >= DateTime.UtcNow.Date)));

            if (hasConflictForClient)
            {
                ModelState.AddModelError(string.Empty, "Client has a conflicting existing appointment.");
                return View(model);
            }

            var slotTaken = await _context.Appointments.AnyAsync(a =>
                a.Id != appointment.Id &&
                a.AppointmentDate == model.AppointmentDate &&
                a.TimeSlotId == model.TimeSlotId &&
                AppointmentStatus.ActiveStatuses.Contains(a.Status));

            if (slotTaken)
            {
                ModelState.AddModelError(nameof(AdminUpdateAppointmentViewModel.TimeSlotId), "Selected slot is already booked.");
                return View(model);
            }

            appointment.Name = model.Name.Trim();
            appointment.PhoneNumber = model.PhoneNumber.Trim();
            appointment.NormalizedPhoneNumber = normalizedPhone;
            appointment.Email = model.Email?.Trim();
            appointment.Age = model.Age;
            appointment.ReasonForVisit = model.ReasonForVisit?.Trim();
            appointment.Address = model.Address?.Trim();
            appointment.AppointmentDate = model.AppointmentDate;
            appointment.TimeSlotId = model.TimeSlotId!.Value;
            appointment.Status = AppointmentStatus.Modified;
            appointment.UpdatedAtUtc = DateTime.UtcNow;
            appointment.ModifiedByAdminId = _userManager.GetUserId(User);

            _context.Entry(appointment).Property(a => a.RowVersion).OriginalValue = model.RowVersion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "This record was updated by another admin. Reload and try again.");
                model.AvailableSlots = await GetSlotOptionsAsync(model.AppointmentDate, model.Id);
                return View(model);
            }

            return RedirectToAction(nameof(ManageAppointments));
        }

        private async Task<IReadOnlyCollection<SelectListItem>> GetSlotOptionsAsync(DateTime date, int appointmentId)
        {
            var reservedIds = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentDate == date && a.Id != appointmentId && AppointmentStatus.ActiveStatuses.Contains(a.Status))
                .Select(a => a.TimeSlotId)
                .ToListAsync();

            var slots = await _context.TimeSlots
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.StartTime)
                .ToListAsync();

            return slots
                .Where(s => !reservedIds.Contains(s.Id))
                .Select(s => new SelectListItem(s.GetDisplayLabel(), s.Id.ToString()))
                .ToList();
        }

        private static string NormalizePhone(string phone)
        {
            var builder = new StringBuilder();
            foreach (var c in phone)
            {
                if (char.IsDigit(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
