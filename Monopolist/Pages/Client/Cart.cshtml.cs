using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie")]
[IgnoreAntiforgeryToken] // Для всех методов на этой странице (или можно ставить на отдельные)
public class CartModel : PageModel
{
    private readonly AppDbContext _context;

    public CartModel(AppDbContext context)
    {
        _context = context;
    }

    public List<CartItemViewModel> CartItems { get; set; } = new();
    public string CustomerName { get; set; } = string.Empty;
    public decimal CustomerDiscount { get; set; }
    public decimal TotalAmount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var customerId = GetCustomerId();
        if (customerId == null) return RedirectToPage("/Account/CustomerLogin");

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return RedirectToPage("/Account/CustomerLogin");

        CustomerName = customer.FullName;
        CustomerDiscount = customer.Discount;

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Warehouse)
            .Where(ci => ci.CustomerId == customerId)
            .ToListAsync();

        CartItems = cartItems.Select(ci => new CartItemViewModel
        {
            Id = ci.Id,
            ProductId = ci.ProductId,
            Name = ci.Product.Name,
            Price = ci.Product.SalePrice,
            Quantity = ci.Quantity,
            Unit = ci.Product.Unit,
            CurrentStock = ci.Product.CurrentStock,
            ImageUrl = ci.Product.ImageUrl ?? (ci.Product.Warehouse != null ? ci.Product.Warehouse.ImageUrl : null)
        }).ToList();

        TotalAmount = CartItems.Sum(i => i.Price * i.Quantity);
        return Page();
    }

    public async Task<IActionResult> OnGetCount()
    {
        var customerId = GetCustomerId();
        if (customerId == null) return new JsonResult(new { count = 0 });
        var count = await _context.CartItems
            .Where(ci => ci.CustomerId == customerId)
            .SumAsync(ci => ci.Quantity);
        return new JsonResult(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> OnPostAddAsync(int productId, int quantity = 1)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized();

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CustomerId == customerId && ci.ProductId == productId);

        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
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

    [HttpPost]
    public async Task<IActionResult> OnPostUpdateQuantityAsync(int cartItemId, int quantity)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized();

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CustomerId == customerId);

        if (cartItem == null) return NotFound();

        if (quantity <= 0)
        {
            _context.CartItems.Remove(cartItem);
        }
        else
        {
            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> OnPostRemoveAsync(int cartItemId)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized();

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
    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized();

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.CustomerId == customerId)
            .ToListAsync();

        if (!cartItems.Any()) return BadRequest("Корзина пуста");

        var orderNumber = await GenerateOrderNumberAsync();

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId.Value,
            OrderDate = DateTime.UtcNow,
            TotalAmount = cartItems.Sum(ci => ci.Product.SalePrice * ci.Quantity),
            Status = "Pending",
            PaymentMethod = null,
            OrderItems = cartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                PriceAtSale = ci.Product.SalePrice
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
        if (claim != null && int.TryParse(claim, out int id)) return id;
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
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = "шт";
    public int CurrentStock { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Total => Price * Quantity;
}