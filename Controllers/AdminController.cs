using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult ManageGlasses()
        {
            return View(_context.Glasses.ToList());
        }

        public IActionResult AddGlass()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddGlass(Glass glass)
        {
            _context.Glasses.Add(glass);
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageGlasses");
        }

        public IActionResult ManageAppointments()
        {
            return View(_context.Appointments.ToList());
        }
    }
}
