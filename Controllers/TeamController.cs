using EyeClinicApp.Helpers;
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
        public async Task<IActionResult> Create(PersonProfile profile, IFormFile? imageFile)
        {
            PopulateRoles(profile.Role);
            if (!ImageUploadHelper.IsValidImageFile(imageFile, out var fileValidationError))
            {
                ModelState.AddModelError("imageFile", fileValidationError);
            }

            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            profile.ProfileImageBase64 = await ImageUploadHelper.ConvertToBase64Async(imageFile);
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
        public async Task<IActionResult> Edit(PersonProfile profile, IFormFile? imageFile)
        {
            PopulateRoles(profile.Role);
            if (!ImageUploadHelper.IsValidImageFile(imageFile, out var fileValidationError))
            {
                ModelState.AddModelError("imageFile", fileValidationError);
            }

            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            var existingProfile = await _teamService.GetByIdAsync(profile.Id);
            if (existingProfile is null)
            {
                return NotFound();
            }

            existingProfile.Name = profile.Name;
            existingProfile.Role = profile.Role;
            existingProfile.Qualification = profile.Qualification;
            existingProfile.ExperienceYears = profile.ExperienceYears;
            existingProfile.Achievements = profile.Achievements;
            existingProfile.Bio = profile.Bio;
            existingProfile.IsActive = profile.IsActive;

            var updatedImageBase64 = await ImageUploadHelper.ConvertToBase64Async(imageFile);
            if (!string.IsNullOrWhiteSpace(updatedImageBase64))
            {
                existingProfile.ProfileImageBase64 = updatedImageBase64;
            }

            await _teamService.UpdateAsync(existingProfile);
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
