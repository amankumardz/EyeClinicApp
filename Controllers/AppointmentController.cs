using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Book()
        {
            return View(new Appointment { AppointmentDate = DateTime.UtcNow.AddDays(1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Appointment model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            if (model.AppointmentDate <= DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(Appointment.AppointmentDate), "Appointment must be scheduled in the future.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.UserId = user.Id;
            model.Status = "Pending";

            _context.Appointments.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyAppointments));
        }

        public async Task<IActionResult> MyAppointments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }
    }
}
