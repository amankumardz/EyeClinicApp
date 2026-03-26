using EyeClinicApp.Data;
using Microsoft.AspNetCore.Mvc;

namespace EyeClinicApp.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var glasses = _context.Glasses.ToList();
            return View(glasses);
        }
    }
}
