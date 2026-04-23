using EyeClinicApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EyeClinicApp.Controllers
{
    [Authorize]
    public class PrescriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PrescriptionController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var prescription = await _context.Prescriptions
                .AsNoTracking()
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.Id == id && p.Appointment != null && p.Appointment.UserId == userId);

            if (prescription is null)
            {
                return NotFound();
            }

            var sourcePath = Path.Combine(_environment.WebRootPath, prescription.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (!System.IO.File.Exists(sourcePath))
            {
                return NotFound();
            }

            var fileName = $"prescription-{prescription.AppointmentId}.pdf";
            var contentType = string.IsNullOrWhiteSpace(prescription.FileContentType)
                ? "application/octet-stream"
                : prescription.FileContentType;

            var bytes = await System.IO.File.ReadAllBytesAsync(sourcePath);
            return File(bytes, contentType, fileName);
        }
    }
}
