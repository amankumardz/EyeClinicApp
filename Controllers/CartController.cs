using EyeClinicApp.Data;
using EyeClinicApp.Models;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EyeClinicApp.Controllers
{
    public class CartController : Controller
    {
        private const string SessionCartKey = "CartItems";
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
                Items = await GetCartRowsAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int glassId, int quantity = 1)
        {
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var glass = await _context.Glasses.AsNoTracking().FirstOrDefaultAsync(g => g.Id == glassId);
            if (glass is null)
            {
                return NotFound();
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var existing = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.GlassId == glassId);
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
            }
            else
            {
                var cart = GetSessionCart();
                var existing = cart.FirstOrDefault(c => c.GlassId == glassId);
                if (existing is null)
                {
                    cart.Add(new SessionCartItem { GlassId = glassId, Quantity = quantity });
                }
                else
                {
                    existing.Quantity += quantity;
                }

                SaveSessionCart(cart);
            }

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

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
                if (item is null)
                {
                    return NotFound();
                }

                item.Quantity = quantity;
                await _context.SaveChangesAsync();
            }
            else
            {
                var cart = GetSessionCart();
                var item = cart.FirstOrDefault(c => c.GlassId == cartItemId);
                if (item is null)
                {
                    return NotFound();
                }

                item.Quantity = quantity;
                SaveSessionCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
                if (item is null)
                {
                    return NotFound();
                }

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            else
            {
                var cart = GetSessionCart();
                cart.RemoveAll(c => c.GlassId == cartItemId);
                SaveSessionCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<CartRowViewModel>> GetCartRowsAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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

            var sessionItems = GetSessionCart();
            if (sessionItems.Count == 0)
            {
                return [];
            }

            var ids = sessionItems.Select(i => i.GlassId).ToList();
            var glasses = await _context.Glasses
                .AsNoTracking()
                .Where(g => ids.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id);

            return sessionItems
                .Where(i => glasses.ContainsKey(i.GlassId))
                .Select(i => new CartRowViewModel
                {
                    CartItemId = i.GlassId,
                    GlassId = i.GlassId,
                    Name = glasses[i.GlassId].Name,
                    ImageBase64 = glasses[i.GlassId].ImageBase64,
                    Price = glasses[i.GlassId].Price,
                    Quantity = i.Quantity
                })
                .ToList();
        }

        private List<SessionCartItem> GetSessionCart()
        {
            var json = HttpContext.Session.GetString(SessionCartKey);
            return string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<SessionCartItem>>(json) ?? [];
        }

        private void SaveSessionCart(List<SessionCartItem> cart)
        {
            HttpContext.Session.SetString(SessionCartKey, JsonSerializer.Serialize(cart));
        }

        private sealed class SessionCartItem
        {
            public int GlassId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
