using EyeClinicApp.Helpers;
using EyeClinicApp.Models;
using EyeClinicApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EyeClinicApp.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewService.GetApprovedTopAsync(50);
            return View(reviews);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var reviews = await _reviewService.GetAdminListAsync();
            return View(reviews);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create() => View(new Review());

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review review, IFormFile? imageFile)
        {
            if (!ImageUploadHelper.IsValidImageFile(imageFile, out var fileValidationError))
            {
                ModelState.AddModelError("imageFile", fileValidationError);
            }

            if (!ModelState.IsValid)
            {
                return View(review);
            }

            review.ClientImageBase64 = await ImageUploadHelper.ConvertToBase64Async(imageFile);
            await _reviewService.CreateAsync(review);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var review = await _reviewService.GetByIdAsync(id);
            if (review is null)
            {
                return NotFound();
            }

            return View(review);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Review review, IFormFile? imageFile)
        {
            if (!ImageUploadHelper.IsValidImageFile(imageFile, out var fileValidationError))
            {
                ModelState.AddModelError("imageFile", fileValidationError);
            }

            if (!ModelState.IsValid)
            {
                return View(review);
            }

            var existingReview = await _reviewService.GetByIdAsync(review.Id);
            if (existingReview is null)
            {
                return NotFound();
            }

            existingReview.ClientName = review.ClientName;
            existingReview.Rating = review.Rating;
            existingReview.ReviewText = review.ReviewText;
            existingReview.IsApproved = review.IsApproved;

            var updatedImageBase64 = await ImageUploadHelper.ConvertToBase64Async(imageFile);
            if (!string.IsNullOrWhiteSpace(updatedImageBase64))
            {
                existingReview.ClientImageBase64 = updatedImageBase64;
            }

            await _reviewService.UpdateAsync(existingReview);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _reviewService.DeleteAsync(id);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            await _reviewService.ToggleApprovalAsync(id);
            return RedirectToAction(nameof(Manage));
        }
    }
}
