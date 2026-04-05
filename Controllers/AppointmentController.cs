using EyeClinicApp.Data;
using EyeClinicApp.Models;
using EyeClinicApp.Services;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;

namespace EyeClinicApp.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentController> _logger;
        private readonly IEmailService _emailService;
        private readonly SmtpSettings _smtpSettings;
        private const string SelectedDateTempKey = "Booking.SelectedDate";
        private const string SelectedSlotTempKey = "Booking.SelectedSlotId";
        private const string ClinicName = "Noida Eye and Medical Centre";
        private const string ClinicAddress = "Major Uday Apartments, 1023, 1021, Mall Road, Sector 29, Noida, Uttar Pradesh 201303";
        private const string ClinicMapLink = "https://www.google.com/maps/search/?api=1&query=Noida+Eye+and+Medical+Centre+Sector+29+Noida";

        public AppointmentController(
            ApplicationDbContext context,
            ILogger<AppointmentController> logger,
            IEmailService emailService,
            IOptions<SmtpSettings> smtpOptions)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _smtpSettings = smtpOptions.Value;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Book(DateTime? date)
        {
            var selectedDate = GetSafeDate(date);
            var model = await BuildSlotSelectionViewModelAsync(selectedDate, null);
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookAppointmentSlotSelectionViewModel model)
        {
            _logger.LogInformation("Book POST fallback received SelectedDate={SelectedDate}, SelectedSlotId={SelectedSlotId}", model.SelectedDate, model.SelectedSlotId);
            return await SelectSlot(model.SelectedDate, model.SelectedSlotId ?? 0);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectSlot(DateTime SelectedDate, int TimeSlotId)
        {
            _logger.LogInformation("SelectSlot POST received SelectedDate={SelectedDate}, TimeSlotId={TimeSlotId}", SelectedDate, TimeSlotId);

            var safeDate = SelectedDate == default ? DateTime.UtcNow.Date : GetSafeDate(SelectedDate);

            if (SelectedDate == default)
            {
                ModelState.AddModelError(nameof(SelectedDate), "Please select a date to continue.");
                ModelState.AddModelError(nameof(BookAppointmentSlotSelectionViewModel.SelectedDate), "Please select a date to continue.");
            }

            if (TimeSlotId == 0)
            {
                ModelState.AddModelError(nameof(TimeSlotId), "Please select one available slot.");
                ModelState.AddModelError(nameof(BookAppointmentSlotSelectionViewModel.SelectedSlotId), "Please select one available slot.");
            }

            if (!ModelState.IsValid)
            {
                var retryModel = await BuildSlotSelectionViewModelAsync(safeDate, TimeSlotId == 0 ? null : TimeSlotId);
                return View("Book", retryModel);
            }

            var slot = await _context.TimeSlots.AsNoTracking().FirstOrDefaultAsync(t => t.Id == TimeSlotId && t.IsActive);
            if (slot is null)
            {
                ModelState.AddModelError(nameof(TimeSlotId), "Selected slot is invalid.");
                ModelState.AddModelError(nameof(BookAppointmentSlotSelectionViewModel.SelectedSlotId), "Selected slot is invalid.");
                var retryModel = await BuildSlotSelectionViewModelAsync(safeDate, TimeSlotId);
                return View("Book", retryModel);
            }

            var alreadyBooked = await _context.Appointments.AsNoTracking().AnyAsync(a =>
                a.AppointmentDate == safeDate &&
                a.TimeSlotId == TimeSlotId &&
                a.Status != AppointmentStatus.Rejected &&
                a.Status != AppointmentStatus.Completed);

            if (alreadyBooked)
            {
                ModelState.AddModelError(nameof(TimeSlotId), "This slot was just booked. Please select another slot.");
                ModelState.AddModelError(nameof(BookAppointmentSlotSelectionViewModel.SelectedSlotId), "This slot was just booked. Please select another slot.");
                var retryModel = await BuildSlotSelectionViewModelAsync(safeDate, null);
                return View("Book", retryModel);
            }

            if (IsSlotExpired(safeDate, slot.StartTime))
            {
                ModelState.AddModelError(nameof(TimeSlotId), "Selected slot has already passed. Please choose an upcoming slot.");
                ModelState.AddModelError(nameof(BookAppointmentSlotSelectionViewModel.SelectedSlotId), "Selected slot has already passed. Please choose an upcoming slot.");
                var retryModel = await BuildSlotSelectionViewModelAsync(safeDate, null);
                return View("Book", retryModel);
            }

            TempData[SelectedDateTempKey] = safeDate.ToString("yyyy-MM-dd");
            TempData[SelectedSlotTempKey] = TimeSlotId.ToString();

            return RedirectToAction(nameof(Confirm), new
            {
                selectedDate = safeDate.ToString("yyyy-MM-dd"),
                timeSlotId = TimeSlotId
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Confirm(DateTime? selectedDate, int? timeSlotId)
        {
            DateTime? selectedDateFromQuery = selectedDate.HasValue ? GetSafeDate(selectedDate) : null;
            var selectedDateFromTempData = ReadSelectedDateFromTempData();
            var selectedDateValue = selectedDateFromQuery ?? selectedDateFromTempData;

            var selectedSlotFromTempData = ReadSelectedSlotFromTempData();
            var selectedSlotId = timeSlotId ?? selectedSlotFromTempData;

            _logger.LogInformation(
                "Confirm GET resolved SelectedDate(query={QueryDate}, temp={TempDate}, final={FinalDate}), TimeSlotId(query={QuerySlot}, temp={TempSlot}, final={FinalSlot})",
                selectedDateFromQuery,
                selectedDateFromTempData,
                selectedDateValue,
                timeSlotId,
                selectedSlotFromTempData,
                selectedSlotId);

            if (!selectedDateValue.HasValue || !selectedSlotId.HasValue)
            {
                TempData["Error"] = "Please select a date and slot to continue.";
                return RedirectToAction(nameof(Book));
            }

            var slot = await _context.TimeSlots.AsNoTracking().FirstOrDefaultAsync(t => t.Id == selectedSlotId.Value && t.IsActive);
            if (slot is null)
            {
                TempData["Error"] = "Selected slot is no longer available.";
                return RedirectToAction(nameof(Book), new { date = selectedDateValue.Value.ToString("yyyy-MM-dd") });
            }

            if (IsSlotExpired(selectedDateValue.Value, slot.StartTime))
            {
                TempData["Error"] = "Selected slot has already passed. Please choose another slot.";
                return RedirectToAction(nameof(Book), new { date = selectedDateValue.Value.ToString("yyyy-MM-dd") });
            }

            KeepSelectionInTempData(selectedDateValue.Value, selectedSlotId.Value);

            var model = new BookAppointmentViewModel
            {
                AppointmentDate = selectedDateValue.Value,
                TimeSlotId = slot.Id,
                SelectedTimeSlotLabel = slot.GetDisplayLabel()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
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
                if (IsSlotExpired(model.AppointmentDate, slot.StartTime))
                {
                    ModelState.AddModelError(nameof(BookAppointmentViewModel.TimeSlotId), "Selected slot has already passed. Please choose an upcoming slot.");
                }

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

            var bookingSlot = await _context.TimeSlots
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == model.TimeSlotId && t.IsActive);

            if (bookingSlot is null || IsSlotExpired(model.AppointmentDate, bookingSlot.StartTime))
            {
                ModelState.AddModelError(nameof(BookAppointmentViewModel.TimeSlotId), "Selected slot is expired or unavailable now. Please choose another slot.");
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

            await SendBookingEmailsAsync(appointment, bookingSlot);

            TempData["Success"] = "Appointment booked successfully. Current status: Pending.";
            TempData.Remove(SelectedDateTempKey);
            TempData.Remove(SelectedSlotTempKey);
            return RedirectToAction(nameof(Book), new { date = model.AppointmentDate.ToString("yyyy-MM-dd") });
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyAppointments()
        {
            var signedInEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(signedInEmail))
            {
                return View(Enumerable.Empty<Appointment>());
            }

            var normalizedEmail = signedInEmail.Trim().ToLowerInvariant();

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.TimeSlot)
                .Where(a => a.Email != null && a.Email.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.CreatedAtUtc)
                .ToListAsync();

            return View(appointments);
        }

        private async Task<BookAppointmentSlotSelectionViewModel> BuildSlotSelectionViewModelAsync(DateTime selectedDate, int? selectedSlotId)
        {
            var localNow = DateTime.Now;
            var today = localNow.Date;
            var tabs = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = today.AddDays(offset);
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
                        IsDisabled = date < today
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
                            IsBooked = bookedSlotIds.Contains(s.Id),
                            IsExpired = IsSlotExpired(selectedDate, s.StartTime, localNow)
                        })
                        .ToList()
                })
                .ToList();

            if (selectedSlotId.HasValue)
            {
                var selectedIsUnavailable = groups.SelectMany(g => g.Slots).Any(s => s.Id == selectedSlotId.Value && (s.IsBooked || s.IsExpired));
                if (selectedIsUnavailable)
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
            var today = DateTime.Now.Date;
            var requested = (date ?? today).Date;
            return requested < today ? today : requested;
        }

        private DateTime? ReadSelectedDateFromTempData()
        {
            if (TempData.Peek(SelectedDateTempKey) is string rawDate && DateTime.TryParse(rawDate, out var parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        private int? ReadSelectedSlotFromTempData()
        {
            if (TempData.Peek(SelectedSlotTempKey) is string rawSlot && int.TryParse(rawSlot, out var slotId))
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

        private static bool IsSlotExpired(DateTime selectedDate, TimeSpan slotStartTime, DateTime? referenceNow = null)
        {
            var now = referenceNow ?? DateTime.Now;
            if (selectedDate.Date != now.Date)
            {
                return false;
            }

            return slotStartTime <= now.TimeOfDay;
        }

        private async Task SendBookingEmailsAsync(Appointment appointment, TimeSlot slot)
        {
            var userEmail = appointment.Email?.Trim();
            var adminEmail = _smtpSettings.AdminEmail?.Trim();
            var slotLabel = slot.GetDisplayLabel();

            var sendTasks = new List<Task>();
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                sendTasks.Add(_emailService.SendEmailAsync(
                    userEmail,
                    "Appointment Confirmed - Noida Eye and Medical Centre",
                    BuildAppointmentEmailHtml(appointment, slotLabel, true)));
            }

            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                sendTasks.Add(_emailService.SendEmailAsync(
                    adminEmail,
                    "New Appointment Booked - Noida Eye and Medical Centre",
                    BuildAppointmentEmailHtml(appointment, slotLabel, false)));
            }

            if (!sendTasks.Any())
            {
                _logger.LogWarning("Booking email skipped because both user email and admin email were empty. AppointmentId={AppointmentId}", appointment.Id);
                return;
            }

            try
            {
                await Task.WhenAll(sendTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Appointment booked but email notification failed. AppointmentId={AppointmentId}", appointment.Id);
            }
        }

        private static string BuildAppointmentEmailHtml(Appointment appointment, string slotLabel, bool isForUser)
        {
            var heading = isForUser ? "Your appointment is confirmed" : "New appointment booked";
            var intro = isForUser
                ? "Thank you for booking with us. Your appointment details are below."
                : "A new appointment has been booked. Please review the details below.";
            var reason = string.IsNullOrWhiteSpace(appointment.ReasonForVisit) ? "N/A" : appointment.ReasonForVisit;

            return $$"""
                     <div style="font-family:Segoe UI,Arial,sans-serif;background:#f6f8fb;padding:24px;">
                         <div style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5eaf2;border-radius:12px;padding:24px;">
                             <h2 style="margin:0 0 8px;color:#0f172a;">{{heading}}</h2>
                             <p style="margin:0 0 20px;color:#334155;">{{intro}}</p>
                             <table style="width:100%;border-collapse:collapse;margin-bottom:18px;">
                                 <tr><td style="padding:8px;border-bottom:1px solid #eef2f7;"><strong>Patient Name</strong></td><td style="padding:8px;border-bottom:1px solid #eef2f7;">{{appointment.Name}}</td></tr>
                                 <tr><td style="padding:8px;border-bottom:1px solid #eef2f7;"><strong>Phone Number</strong></td><td style="padding:8px;border-bottom:1px solid #eef2f7;">{{appointment.PhoneNumber}}</td></tr>
                                 <tr><td style="padding:8px;border-bottom:1px solid #eef2f7;"><strong>Appointment Date</strong></td><td style="padding:8px;border-bottom:1px solid #eef2f7;color:#0b7a33;"><strong>{{appointment.AppointmentDate:dd MMM yyyy}}</strong></td></tr>
                                 <tr><td style="padding:8px;border-bottom:1px solid #eef2f7;"><strong>Time Slot</strong></td><td style="padding:8px;border-bottom:1px solid #eef2f7;color:#0b7a33;"><strong>{{slotLabel}}</strong></td></tr>
                                 <tr><td style="padding:8px;border-bottom:1px solid #eef2f7;"><strong>Reason</strong></td><td style="padding:8px;border-bottom:1px solid #eef2f7;">{{reason}}</td></tr>
                             </table>
                             <div style="background:#f1f8ff;border:1px solid #d5e7fb;border-radius:10px;padding:14px;">
                                 <div style="font-weight:700;color:#0f172a;margin-bottom:4px;">{{ClinicName}}</div>
                                 <div style="color:#334155;margin-bottom:8px;">{{ClinicAddress}}</div>
                                 <a href="{{ClinicMapLink}}" style="color:#0b5ed7;font-weight:600;text-decoration:none;">View clinic on Google Maps</a>
                             </div>
                         </div>
                     </div>
                     """;
        }
    }
}
