using EyeClinicApp.Models;
using EyeClinicApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EyeClinicApp.Controllers
{
    public class TeamController : Controller
    {
        private readonly ITeamService _teamService;

        public TeamController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var profiles = await _teamService.GetActiveProfilesAsync();
            return View(profiles);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var profiles = await _teamService.GetAdminListAsync();
            return View(profiles);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            PopulateRoles();
            return View(new PersonProfile());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PersonProfile profile)
        {
            PopulateRoles(profile.Role);
            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            await _teamService.CreateAsync(profile);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var profile = await _teamService.GetByIdAsync(id);
            if (profile is null)
            {
                return NotFound();
            }

            PopulateRoles(profile.Role);
            return View(profile);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PersonProfile profile)
        {
            PopulateRoles(profile.Role);
            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            await _teamService.UpdateAsync(profile);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _teamService.DeleteAsync(id);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            await _teamService.ToggleStatusAsync(id);
            return RedirectToAction(nameof(Manage));
        }

        private void PopulateRoles(string? selectedRole = null)
        {
            ViewBag.Roles = PersonProfileRole.AllowedRoles
                .Select(role => new SelectListItem(role, role, role == selectedRole))
                .ToList();
        }
    }
}
