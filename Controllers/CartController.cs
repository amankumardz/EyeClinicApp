using EyeClinicApp.Data;
using EyeClinicApp.Models;
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

        public CartController(ApplicationDbContext context)
        {
            _context = context;
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
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    GlassId = glassId,
                    Quantity = quantity
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
                    Quantity = c.Quantity
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
