using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalGlasses = await _context.Glasses.CountAsync();
            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
            ViewBag.PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == "Pending");
            return View();
        }

        public async Task<IActionResult> ManageGlasses()
        {
            var glasses = await _context.Glasses.AsNoTracking().OrderBy(g => g.Brand).ThenBy(g => g.Name).ToListAsync();
            return View(glasses);
        }

        public IActionResult AddGlass()
        {
            return View(new Glass());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGlass(Glass glass)
        {
            if (!ModelState.IsValid)
            {
                return View(glass);
            }

            _context.Glasses.Add(glass);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageGlasses));
        }

        public async Task<IActionResult> ManageAppointments()
        {
            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.User)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }
    }
}
