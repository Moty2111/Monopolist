using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie, GuestCookie")]
[IgnoreAntiforgeryToken]
public class CartModel : PageModel
{
    private readonly AppDbContext _context;

    public CartModel(AppDbContext context)
    {
        _context = context;
    }

    public List<CartItemViewModel> CartItems { get; set; } = new();
    public string CustomerName { get; set; } = "Гость";
    public string? AvatarUrl { get; set; }
    public decimal CustomerDiscount { get; set; }
    public bool IsGuest { get; private set; }
    public decimal TotalAmount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        IsGuest = role != "Customer";

        if (!IsGuest)
        {
            var customerId = GetCustomerId();
            if (customerId != null && customerId > 0)
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer != null)
                {
                    CustomerName = customer.FullName;
                    CustomerDiscount = customer.Discount;
                    AvatarUrl = customer.AvatarUrl;
                }

                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                        .ThenInclude(p => p.Warehouse)
                    .Where(ci => ci.CustomerId == customerId)
                    .ToListAsync();

                var discountFactor = 1 - (CustomerDiscount / 100m);

                CartItems = cartItems.Select(ci => new CartItemViewModel
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    Name = ci.Product.Name,
                    OriginalPrice = ci.Product.SalePrice,
                    Price = ci.Product.SalePrice * discountFactor,
                    Quantity = ci.Quantity,
                    Unit = ci.Product.Unit,
                    CurrentStock = ci.Product.CurrentStock,
                    ImageUrl = ci.Product.ImageUrl ?? (ci.Product.Warehouse != null ? ci.Product.Warehouse.ImageUrl : null)
                }).ToList();

                TotalAmount = CartItems.Sum(i => i.Price * i.Quantity);
            }
            else
            {
                IsGuest = true;
                CustomerName = "Гость";
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnGetCount()
    {
        if (IsGuest) return new JsonResult(new { count = 0 });
        var customerId = GetCustomerId();
        if (customerId == null || customerId <= 0) return new JsonResult(new { count = 0 });
        var count = await _context.CartItems
            .Where(ci => ci.CustomerId == customerId)
            .SumAsync(ci => ci.Quantity);
        return new JsonResult(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> OnPostAddAsync(int productId, int quantity = 1)
    {
        if (IsGuest) return Unauthorized();
        var customerId = GetCustomerId();
        if (customerId == null || customerId <= 0) return Unauthorized();

        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound("Товар не найден");
        if (product.CurrentStock < quantity) return BadRequest("Недостаточно товара на складе");

        try
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CustomerId == customerId && ci.ProductId == productId);

            if (cartItem != null)
            {
                int newQuantity = cartItem.Quantity + quantity;
                if (product.CurrentStock < newQuantity)
                    return BadRequest("Недостаточно товара на складе");
                cartItem.Quantity = newQuantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                cartItem = new CartItem
                {
                    CustomerId = customerId.Value,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
        {
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CustomerId == customerId && ci.ProductId == productId);
            if (existing != null)
            {
                int newQuantity = existing.Quantity + quantity;
                if (product.CurrentStock < newQuantity)
                    return BadRequest("Недостаточно товара на складе");
                existing.Quantity = newQuantity;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }
    }

    [HttpPost]
    public async Task<IActionResult> OnPostUpdateQuantityAsync(int cartItemId, int quantity)
    {
        if (IsGuest) return Unauthorized();
        var customerId = GetCustomerId();
        if (customerId == null || customerId <= 0) return Unauthorized();

        var cartItem = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CustomerId == customerId);

        if (cartItem == null) return NotFound();

        if (quantity <= 0)
        {
            _context.CartItems.Remove(cartItem);
        }
        else
        {
            if (cartItem.Product.CurrentStock < quantity)
                return BadRequest("Недостаточно товара на складе");
            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> OnPostRemoveAsync(int cartItemId)
    {
        if (IsGuest) return Unauthorized();
        var customerId = GetCustomerId();
        if (customerId == null || customerId <= 0) return Unauthorized();

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CustomerId == customerId);

        if (cartItem != null)
        {
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
        }
        return new JsonResult(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> OnPostCheckoutAsync([FromBody] CheckoutRequest request)
    {
        if (request == null) return BadRequest("Не указан способ оплаты");

        if (IsGuest) return Unauthorized();
        var customerId = GetCustomerId();
        if (customerId == null || customerId <= 0) return Unauthorized();

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.CustomerId == customerId)
            .ToListAsync();

        if (!cartItems.Any()) return BadRequest("Корзина пуста");

        var customer = await _context.Customers.FindAsync(customerId);
        var discountFactor = 1 - (customer?.Discount ?? 0) / 100m;

        foreach (var item in cartItems)
        {
            if (item.Product.CurrentStock < item.Quantity)
                return BadRequest($"Товар \"{item.Product.Name}\" в количестве {item.Quantity} шт. отсутствует на складе. Доступно: {item.Product.CurrentStock}");
        }

        var validMethods = new[] { "Card", "Cash", "Credit" };
        if (!validMethods.Contains(request.PaymentMethod))
            return BadRequest("Неверный способ оплаты");

        var orderNumber = await GenerateOrderNumberAsync();

        foreach (var item in cartItems)
        {
            item.Product.CurrentStock -= item.Quantity;
        }

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId.Value,
            OrderDate = DateTime.UtcNow,
            TotalAmount = cartItems.Sum(ci => ci.Product.SalePrice * ci.Quantity * discountFactor),
            Status = "Pending",
            PaymentMethod = request.PaymentMethod,
            OrderItems = cartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                PriceAtSale = ci.Product.SalePrice * discountFactor
            }).ToList()
        };

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true, orderId = order.Id });
    }

    private int? GetCustomerId()
    {
        var claim = User.FindFirst("CustomerId")?.Value;
        if (claim != null && int.TryParse(claim, out int id) && id > 0) return id;
        return null;
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = $"ORD-{today:yyyyMMdd}-";
        var lastOrder = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastOrder))
        {
            var numStr = lastOrder[prefix.Length..];
            if (int.TryParse(numStr, out int num)) nextNumber = num + 1;
        }
        return $"{prefix}{nextNumber:D3}";
    }
}

public class CartItemViewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = "шт";
    public int CurrentStock { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Total => Price * Quantity;
}

public class CheckoutRequest
{
    public string PaymentMethod { get; set; } = "Card";
}