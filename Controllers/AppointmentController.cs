using EyeClinicApp.Data;
using EyeClinicApp.Models;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace EyeClinicApp.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string SelectedDateTempKey = "Booking.SelectedDate";
        private const string SelectedSlotTempKey = "Booking.SelectedSlotId";

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Book(DateTime? date)
        {
            var selectedDate = GetSafeDate(date);
            var model = await BuildSlotSelectionViewModelAsync(selectedDate, null);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookAppointmentSlotSelectionViewModel model)
        {
            model.SelectedDate = GetSafeDate(model.SelectedDate);

            if (!model.SelectedSlotId.HasValue)
            {
                ModelState.AddModelError(nameof(BookAppointmentSlotSelectionViewModel.SelectedSlotId), "Please select one available slot.");
            }

            if (!ModelState.IsValid)
            {
                var retryModel = await BuildSlotSelectionViewModelAsync(model.SelectedDate, model.SelectedSlotId);
                return View(retryModel);
            }

            var slot = await _context.TimeSlots.AsNoTracking().FirstOrDefaultAsync(t => t.Id == model.SelectedSlotId.Value && t.IsActive);
            if (slot is null)
            {
                ModelState.AddModelError(nameof(BookAppointmentSlotSelectionViewModel.SelectedSlotId), "Selected slot is invalid.");
                var retryModel = await BuildSlotSelectionViewModelAsync(model.SelectedDate, model.SelectedSlotId);
                return View(retryModel);
            }

            var alreadyBooked = await _context.Appointments.AsNoTracking().AnyAsync(a =>
                a.AppointmentDate == model.SelectedDate &&
                a.TimeSlotId == model.SelectedSlotId.Value &&
                a.Status != AppointmentStatus.Rejected &&
                a.Status != AppointmentStatus.Completed);

            if (alreadyBooked)
            {
                ModelState.AddModelError(nameof(BookAppointmentSlotSelectionViewModel.SelectedSlotId), "This slot was just booked. Please select another slot.");
                var retryModel = await BuildSlotSelectionViewModelAsync(model.SelectedDate, null);
                return View(retryModel);
            }

            TempData[SelectedDateTempKey] = model.SelectedDate.ToString("yyyy-MM-dd");
            TempData[SelectedSlotTempKey] = model.SelectedSlotId.Value.ToString();

            return RedirectToAction(nameof(Confirm));
        }

        [HttpGet]
        public async Task<IActionResult> Confirm()
        {
            var selectedDate = ReadSelectedDateFromTempData();
            var selectedSlotId = ReadSelectedSlotFromTempData();

            if (!selectedDate.HasValue || !selectedSlotId.HasValue)
            {
                TempData["Error"] = "Please select a date and slot to continue.";
                return RedirectToAction(nameof(Book));
            }

            var slot = await _context.TimeSlots.AsNoTracking().FirstOrDefaultAsync(t => t.Id == selectedSlotId.Value && t.IsActive);
            if (slot is null)
            {
                TempData["Error"] = "Selected slot is no longer available.";
                return RedirectToAction(nameof(Book), new { date = selectedDate.Value.ToString("yyyy-MM-dd") });
            }

            KeepSelectionInTempData(selectedDate.Value, selectedSlotId.Value);

            var model = new BookAppointmentViewModel
            {
                AppointmentDate = selectedDate.Value,
                TimeSlotId = slot.Id,
                SelectedTimeSlotLabel = slot.GetDisplayLabel()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(BookAppointmentViewModel model)
        {
            model.AppointmentDate = model.AppointmentDate.Date;
            var normalizedPhone = NormalizePhone(model.PhoneNumber);

            if (model.AppointmentDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(BookAppointmentViewModel.AppointmentDate), "Past dates are not allowed.");
            }

            if (normalizedPhone.Length < 7)
            {
                ModelState.AddModelError(nameof(BookAppointmentViewModel.PhoneNumber), "Please enter a valid phone number.");
            }

            var slot = await _context.TimeSlots.AsNoTracking().FirstOrDefaultAsync(t => t.Id == model.TimeSlotId && t.IsActive);
            if (slot is null)
            {
                ModelState.AddModelError(nameof(BookAppointmentViewModel.TimeSlotId), "Selected slot is invalid.");
            }
            else
            {
                model.SelectedTimeSlotLabel = slot.GetDisplayLabel();
            }

            if (!ModelState.IsValid)
            {
                KeepSelectionInTempData(model.AppointmentDate, model.TimeSlotId);
                return View(model);
            }

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var slotTaken = await _context.Appointments.AnyAsync(a =>
                a.AppointmentDate == model.AppointmentDate &&
                a.TimeSlotId == model.TimeSlotId &&
                a.Status != AppointmentStatus.Rejected &&
                a.Status != AppointmentStatus.Completed);

            if (slotTaken)
            {
                ModelState.AddModelError(nameof(BookAppointmentViewModel.TimeSlotId), "Selected slot was just booked. Please choose a different slot.");
                await tx.RollbackAsync();
                KeepSelectionInTempData(model.AppointmentDate, model.TimeSlotId);
                return View(model);
            }

            var hasActiveBooking = await _context.Appointments.AnyAsync(a =>
                a.NormalizedPhoneNumber == normalizedPhone &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved));

            if (hasActiveBooking)
            {
                ModelState.AddModelError(string.Empty, "You already have an active (pending/approved) appointment.");
                await tx.RollbackAsync();
                KeepSelectionInTempData(model.AppointmentDate, model.TimeSlotId);
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
                TimeSlotId = model.TimeSlotId,
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
                ModelState.AddModelError(nameof(BookAppointmentViewModel.TimeSlotId), "The selected slot is no longer available.");
                KeepSelectionInTempData(model.AppointmentDate, model.TimeSlotId);
                return View(model);
            }

            TempData["Success"] = "Appointment booked successfully. Current status: Pending.";
            TempData.Remove(SelectedDateTempKey);
            TempData.Remove(SelectedSlotTempKey);
            return RedirectToAction(nameof(Book), new { date = model.AppointmentDate.ToString("yyyy-MM-dd") });
        }

        private async Task<BookAppointmentSlotSelectionViewModel> BuildSlotSelectionViewModelAsync(DateTime selectedDate, int? selectedSlotId)
        {
            var tabs = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = DateTime.UtcNow.Date.AddDays(offset);
                    return new DateTabViewModel
                    {
                        Date = date,
                        Label = offset switch
                        {
                            0 => "Today",
                            1 => "Tomorrow",
                            _ => date.ToString("dd MMM")
                        },
                        IsSelected = date == selectedDate,
                        IsDisabled = date < DateTime.UtcNow.Date
                    };
                })
                .ToList();

            var slots = await _context.TimeSlots
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.StartTime)
                .ToListAsync();

            var bookedSlotIds = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentDate == selectedDate &&
                    a.Status != AppointmentStatus.Rejected &&
                    a.Status != AppointmentStatus.Completed)
                .Select(a => a.TimeSlotId)
                .ToListAsync();

            var groups = TimeSlotShift.Ordered
                .Select(shift => new ShiftSlotGroupViewModel
                {
                    Shift = shift,
                    Slots = slots
                        .Where(s => s.Shift == shift)
                        .Select(s => new SlotItemViewModel
                        {
                            Id = s.Id,
                            Label = s.StartTime.ToString(@"hh\:mm"),
                            IsBooked = bookedSlotIds.Contains(s.Id)
                        })
                        .ToList()
                })
                .ToList();

            if (selectedSlotId.HasValue)
            {
                var selectedIsBooked = groups.SelectMany(g => g.Slots).Any(s => s.Id == selectedSlotId.Value && s.IsBooked);
                if (selectedIsBooked)
                {
                    selectedSlotId = null;
                }
            }

            return new BookAppointmentSlotSelectionViewModel
            {
                SelectedDate = selectedDate,
                SelectedSlotId = selectedSlotId,
                DateTabs = tabs,
                ShiftGroups = groups
            };
        }

        private static DateTime GetSafeDate(DateTime? date)
        {
            var requested = (date ?? DateTime.UtcNow.Date).Date;
            return requested < DateTime.UtcNow.Date ? DateTime.UtcNow.Date : requested;
        }

        private DateTime? ReadSelectedDateFromTempData()
        {
            if (TempData[SelectedDateTempKey] is string rawDate && DateTime.TryParse(rawDate, out var parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        private int? ReadSelectedSlotFromTempData()
        {
            if (TempData[SelectedSlotTempKey] is string rawSlot && int.TryParse(rawSlot, out var slotId))
            {
                return slotId;
            }

            return null;
        }

        private void KeepSelectionInTempData(DateTime date, int slotId)
        {
            TempData[SelectedDateTempKey] = date.ToString("yyyy-MM-dd");
            TempData[SelectedSlotTempKey] = slotId.ToString();
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
