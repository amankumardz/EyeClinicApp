using EyeClinicApp.Data;
using EyeClinicApp.Models;
using EyeClinicApp.Services;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using AppOrder = EyeClinicApp.Models.Order;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

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
                Items = items,
                RazorpayKeyId = _configuration["Razorpay:KeyId"] ?? string.Empty,
                UpiQrImageUrl = _configuration["Payments:UpiQrImageUrl"] ?? string.Empty,
                UpiId = _configuration["Payments:UpiId"] ?? string.Empty
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
            model.RazorpayKeyId = _configuration["Razorpay:KeyId"] ?? string.Empty;
            model.UpiQrImageUrl = _configuration["Payments:UpiQrImageUrl"] ?? string.Empty;
            model.UpiId = _configuration["Payments:UpiId"] ?? string.Empty;

            if (!PaymentMethod.All.Contains(model.PaymentMethod))
            {
                ModelState.AddModelError(nameof(model.PaymentMethod), "Please choose a valid payment method.");
            }

            if (model.PaymentMethod == PaymentMethod.Razorpay)
            {
                ModelState.AddModelError(nameof(model.PaymentMethod), "Use the Razorpay button to complete this payment method.");
            }

            if (model.PaymentMethod == PaymentMethod.UpiQr && !model.IsUpiPaymentAcknowledged)
            {
                ModelState.AddModelError(nameof(model.IsUpiPaymentAcknowledged), "Please confirm UPI payment acknowledgement to continue.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var order = new AppOrder
            {
                UserId = userId,
                Name = model.Name.Trim(),
                Phone = model.Phone.Trim(),
                Email = model.Email.Trim(),
                Address = model.Address.Trim(),
                Status = OrderStatus.Confirmed,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = model.PaymentMethod == PaymentMethod.CashOnDelivery
                    ? OrderPaymentStatus.Pending
                    : OrderPaymentStatus.AwaitingConfirmation,
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

            await SendOrderEmailsAsync(order, items);

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRazorpayOrder([FromForm] CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = await BuildCartRowsForUserAsync(userId!);
            model.Items = items;

            if (items.Count == 0)
            {
                return BadRequest(new { error = "Your cart is empty." });
            }

            if (model.PaymentMethod != PaymentMethod.Razorpay)
            {
                return BadRequest(new { error = "Please choose Razorpay for this flow." });
            }

            if (!TryValidateModel(model))
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return BadRequest(new { error = string.Join(" ", errors) });
            }

            var keyId = _configuration["Razorpay:KeyId"];
            var keySecret = _configuration["Razorpay:KeySecret"];
            if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(keySecret))
            {
                return StatusCode(500, new { error = "Razorpay is not configured. Please contact support." });
            }

            var order = new AppOrder
            {
                UserId = userId,
                Name = model.Name.Trim(),
                Phone = model.Phone.Trim(),
                Email = model.Email.Trim(),
                Address = model.Address.Trim(),
                Status = OrderStatus.Pending,
                PaymentMethod = PaymentMethod.Razorpay,
                PaymentStatus = OrderPaymentStatus.Pending,
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
            await _context.SaveChangesAsync();

            var client = new RazorpayClient(keyId, keySecret);
            var options = new Dictionary<string, object>
            {
                { "amount", (int)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero) },
                { "currency", "INR" },
                { "receipt", "order_rcptid_" + order.Id }
            };

            var razorpayOrder = client.Order.Create(options);
            order.RazorpayOrderId = razorpayOrder["id"]?.ToString();
            await _context.SaveChangesAsync();

            return Json(new
            {
                key = keyId,
                amount = options["amount"],
                currency = "INR",
                razorpayOrderId = order.RazorpayOrderId,
                internalOrderId = order.Id,
                customer = new { name = order.Name, email = order.Email, contact = order.Phone }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRazorpayPayment([FromBody] RazorpayVerificationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId && o.PaymentMethod == PaymentMethod.Razorpay);

            if (order is null)
            {
                return NotFound(new { error = "Order not found." });
            }

            if (!VerifyRazorpaySignature(request, _configuration["Razorpay:KeySecret"] ?? string.Empty))
            {
                order.PaymentStatus = OrderPaymentStatus.Failed;
                await _context.SaveChangesAsync();
                return BadRequest(new { error = "Payment signature verification failed." });
            }

            order.PaymentId = request.RazorpayPaymentId;
            order.RazorpayOrderId = request.RazorpayOrderId;
            order.PaymentStatus = OrderPaymentStatus.Paid;
            order.Status = OrderStatus.Confirmed;

            var cartItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            await SendOrderEmailsAsync(order, await BuildCartRowsForOrderAsync(order.Id));

            return Json(new { success = true, redirectUrl = Url.Action(nameof(Confirmation), new { id = order.Id }) });
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

        private async Task<List<CartRowViewModel>> BuildCartRowsForOrderAsync(int orderId)
        {
            return await _context.OrderItems
                .AsNoTracking()
                .Include(i => i.Glass)
                .Where(i => i.OrderId == orderId)
                .Select(i => new CartRowViewModel
                {
                    GlassId = i.GlassId,
                    Name = i.Glass!.Name,
                    ImageBase64 = i.Glass!.ImageBase64,
                    Price = i.Price,
                    Quantity = i.Quantity
                })
                .ToListAsync();
        }

        private bool VerifyRazorpaySignature(RazorpayVerificationRequest request, string keySecret)
        {
            if (string.IsNullOrWhiteSpace(keySecret))
            {
                return false;
            }

            var payload = $"{request.RazorpayOrderId}|{request.RazorpayPaymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var generatedSignature = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            return string.Equals(generatedSignature, request.RazorpaySignature, StringComparison.OrdinalIgnoreCase);
        }

        private async Task SendOrderEmailsAsync(AppOrder order, List<CartRowViewModel> items)
        {
            var itemRows = string.Join(string.Empty, items.Select(i => $"<tr><td>{i.Name}</td><td>{i.Quantity}</td><td>{i.Price:C}</td></tr>"));
            var emailBody = $@"<h2>Order Confirmation #{order.Id}</h2>
<p>Thank you, {order.Name}. Your order has been placed.</p>
<table border='1' cellpadding='6' cellspacing='0'>
<thead><tr><th>Item</th><th>Qty</th><th>Price</th></tr></thead>
<tbody>{itemRows}</tbody>
</table>
<p><strong>Total:</strong> {order.TotalAmount:C}</p>
<p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
<p><strong>Payment Status:</strong> {order.PaymentStatus}</p>
<p><strong>Shipping Address:</strong> {order.Address}</p>";

            await _emailService.SendEmailAsync(order.Email, $"Order #{order.Id} Confirmation", emailBody);

            var adminEmail = _configuration["SmtpSettings:Email"];
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                await _emailService.SendEmailAsync(adminEmail, $"New Order #{order.Id}",
                    $"<h2>New order placed</h2><p>Order ID: {order.Id}</p><p>Total: {order.TotalAmount:C}</p><p>Payment: {order.PaymentMethod} ({order.PaymentStatus})</p><p>Customer: {order.Name} ({order.Email})</p>");
            }
        }
    }

    public class RazorpayVerificationRequest
    {
        public int OrderId { get; set; }
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
    }
}
