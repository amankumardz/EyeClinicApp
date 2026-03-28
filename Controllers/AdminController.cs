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
            ViewData["GlassesCount"] = await _context.Glasses.CountAsync();
            ViewData["AppointmentsCount"] = await _context.Appointments.CountAsync();
            return View();
        }

        public async Task<IActionResult> ManageGlasses()
        {
            return View(await _context.Glasses.AsNoTracking().ToListAsync());
        }

        public IActionResult AddGlass()
        {
            return View();
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
            return View(await _context.Appointments.AsNoTracking().OrderByDescending(a => a.AppointmentDate).ToListAsync());
        }
    }
}
