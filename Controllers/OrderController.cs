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
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public OrderController(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = await BuildCartRowsForUserAsync(userId!);

            if (items.Count == 0)
            {
                TempData["CartMessage"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                Name = User.FindFirstValue("FullName") ?? string.Empty,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                Items = items
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = await BuildCartRowsForUserAsync(userId!);

            if (items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Your cart is empty.");
            }

            model.Items = items;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var order = new Order
            {
                UserId = userId,
                Name = model.Name.Trim(),
                Phone = model.Phone.Trim(),
                Email = model.Email.Trim(),
                Address = model.Address.Trim(),
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = items.Sum(i => i.LineTotal),
                Items = items.Select(i => new OrderItem
                {
                    GlassId = i.GlassId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            _context.Orders.Add(order);

            var cartItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            var itemRows = string.Join(string.Empty, items.Select(i => $"<tr><td>{i.Name}</td><td>{i.Quantity}</td><td>{i.Price:C}</td></tr>"));
            var emailBody = $@"<h2>Order Confirmation #{order.Id}</h2>
<p>Thank you, {order.Name}. Your order has been placed.</p>
<table border='1' cellpadding='6' cellspacing='0'>
<thead><tr><th>Item</th><th>Qty</th><th>Price</th></tr></thead>
<tbody>{itemRows}</tbody>
</table>
<p><strong>Total:</strong> {order.TotalAmount:C}</p>
<p><strong>Shipping Address:</strong> {order.Address}</p>";

            await _emailService.SendEmailAsync(order.Email, $"Order #{order.Id} Confirmation", emailBody);

            var adminEmail = _configuration["SmtpSettings:Email"];
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                await _emailService.SendEmailAsync(adminEmail, $"New Order #{order.Id}",
                    $"<h2>New order placed</h2><p>Order ID: {order.Id}</p><p>Total: {order.TotalAmount:C}</p><p>Customer: {order.Name} ({order.Email})</p>");
            }

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order is null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.Glass)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var model = new MyOrdersViewModel
            {
                Orders = orders.Select(o => new MyOrderRowViewModel
                {
                    OrderId = o.Id,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    Items = o.Items.Select(i => new MyOrderItemRowViewModel
                    {
                        Name = i.Glass?.Name ?? "Item",
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        private async Task<List<CartRowViewModel>> BuildCartRowsForUserAsync(string userId)
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
    }
}
