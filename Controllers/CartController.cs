using EyeClinicApp.Data;
using EyeClinicApp.Models;
using EyeClinicApp.Services;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EyeClinicApp.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPrescriptionService _prescriptionService;

        public CartController(ApplicationDbContext context, IPrescriptionService prescriptionService)
        {
            _context = context;
            _prescriptionService = prescriptionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new CartPageViewModel
            {
                Items = await GetCartRowsAsync(GetUserId())
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int glassId, int quantity = 1)
        {
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var glassExists = await _context.Glasses
                .AsNoTracking()
                .AnyAsync(g => g.Id == glassId);

            if (!glassExists)
            {
                return NotFound();
            }

            var userId = GetUserId();
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.GlassId == glassId);

            if (existing is null)
            {
                var latestPrescription = await _prescriptionService.GetLatestForUserAsync(userId);
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    GlassId = glassId,
                    Quantity = quantity,
                    RightEyeSph = latestPrescription?.RightEyeSph,
                    RightEyeCyl = latestPrescription?.RightEyeCyl,
                    RightEyeAxis = latestPrescription?.RightEyeAxis,
                    LeftEyeSph = latestPrescription?.LeftEyeSph,
                    LeftEyeCyl = latestPrescription?.LeftEyeCyl,
                    LeftEyeAxis = latestPrescription?.LeftEyeAxis
                });
            }
            else
            {
                existing.Quantity += quantity;
            }

            await _context.SaveChangesAsync();

            TempData["CartMessage"] = "Item added to cart.";
            return RedirectToAction("Index", "Catalog");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var userId = GetUserId();
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (item is null)
            {
                return NotFound();
            }

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = GetUserId();
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (item is null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLens(int cartItemId, string? rightEyeSph, string? rightEyeCyl, string? rightEyeAxis, string? leftEyeSph, string? leftEyeCyl, string? leftEyeAxis)
        {
            var userId = GetUserId();
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item is null)
            {
                return NotFound();
            }

            item.RightEyeSph = rightEyeSph?.Trim();
            item.RightEyeCyl = rightEyeCyl?.Trim();
            item.RightEyeAxis = rightEyeAxis?.Trim();
            item.LeftEyeSph = leftEyeSph?.Trim();
            item.LeftEyeCyl = leftEyeCyl?.Trim();
            item.LeftEyeAxis = leftEyeAxis?.Trim();
            await _context.SaveChangesAsync();

            TempData["CartMessage"] = "Lens power updated.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<CartRowViewModel>> GetCartRowsAsync(string userId)
        {
            return await _context.CartItems
                .AsNoTracking()
                .Include(c => c.Glass)
                .Where(c => c.UserId == userId)
                .Select(c => new CartRowViewModel
                {
                    CartItemId = c.Id,
                    GlassId = c.GlassId,
                    Name = c.Glass!.Name,
                    ImageBase64 = c.Glass!.ImageBase64,
                    Price = c.Glass!.Price,
                    Quantity = c.Quantity,
                    RightEyeSph = c.RightEyeSph,
                    RightEyeCyl = c.RightEyeCyl,
                    RightEyeAxis = c.RightEyeAxis,
                    LeftEyeSph = c.LeftEyeSph,
                    LeftEyeCyl = c.LeftEyeCyl,
                    LeftEyeAxis = c.LeftEyeAxis
                })
                .ToListAsync();
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Authenticated user id was not found.");
        }
    }
}
