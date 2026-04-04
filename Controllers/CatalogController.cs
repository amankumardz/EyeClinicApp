using EyeClinicApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Controllers
{
    public class CatalogController : Controller
    {
        private static readonly string[] AllowedCategories = ["Men", "Women", "Kids"];
        private readonly ApplicationDbContext _context;

        public CatalogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? category)
        {
            var normalizedCategory = AllowedCategories.FirstOrDefault(c =>
                string.Equals(c, category, StringComparison.OrdinalIgnoreCase));

            var glassesQuery = _context.Glasses
                .AsNoTracking()
                .OrderBy(g => g.Brand)
                .ThenBy(g => g.Name)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedCategory))
            {
                glassesQuery = glassesQuery.Where(g => g.Category == normalizedCategory);
            }

            var glasses = await glassesQuery.ToListAsync();
            ViewBag.SelectedCategory = normalizedCategory;
            ViewBag.Categories = AllowedCategories;
            return View(glasses);
        }
    }
}
