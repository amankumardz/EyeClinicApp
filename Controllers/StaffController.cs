using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Controllers
{
    [Authorize(Roles = AppRoles.Staff)]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.TimeSlot)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.CreatedAtUtc)
                .Take(100)
                .ToListAsync();

            return View(appointments);
        }
    }
}
