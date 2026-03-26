using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Book(Appointment model)
        {
            var user = await _userManager.GetUserAsync(User);

            model.UserId = user.Id;
            model.Status = "Pending";

            _context.Appointments.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyAppointments");
        }

        public async Task<IActionResult> MyAppointments()
        {
            var user = await _userManager.GetUserAsync(User);

            var appointments = _context.Appointments
                .Where(a => a.UserId == user.Id)
                .ToList();

            return View(appointments);
        }
    }
}
