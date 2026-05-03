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

    public CartModel(AppDbContext context) => _context = context;

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
                    AvatarUrl = customer.AvatarUrl;

                    // Быстрый пересчёт лояльности перед показом корзины
                    await UpdateLoyaltyDiscountFast(customer);
                    CustomerDiscount = customer.EffectiveDiscount;
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
                    ImageUrl = ci.Product.ImageUrl ?? (ci.Product.Warehouse?.ImageUrl)
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

        // Ищем существующую запись (без вызова UpdateLoyalty – не нужен)
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CustomerId == customerId && ci.ProductId == productId);

        if (cartItem != null)
        {
            int newQty = cartItem.Quantity + quantity;
            if (product.CurrentStock < newQty) return BadRequest("Недостаточно товара на складе");
            cartItem.Quantity = newQty;
            cartItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        try
        {
            var newItem = new CartItem
            {
                CustomerId = customerId.Value,
                ProductId = productId,
                Quantity = quantity
            };
            _context.CartItems.Add(newItem);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
        {
            // Конфликт: другая сессия уже добавила этот товар
            var entry = _context.ChangeTracker.Entries<CartItem>()
                .FirstOrDefault(e => e.Entity.CustomerId == customerId && e.Entity.ProductId == productId);
            entry.State = EntityState.Detached;

            // Загружаем реальную запись и увеличиваем количество
            cartItem = await _context.CartItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.CustomerId == customerId && ci.ProductId == productId);

            if (cartItem != null)
            {
                _context.CartItems.Attach(cartItem);
                int newQty = cartItem.Quantity + quantity;
                if (product.CurrentStock < newQty) return BadRequest("Недостаточно товара на складе");
                cartItem.Quantity = newQty;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }

            throw; // что-то пошло не так
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
            _context.CartItems.Remove(cartItem);
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

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return Unauthorized();

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.CustomerId == customerId)
            .ToListAsync();

        if (!cartItems.Any()) return BadRequest("Корзина пуста");

        // Один раз обновляем лояльность перед расчётом скидки
        await UpdateLoyaltyDiscountFast(customer);
        var discountFactor = 1 - (customer.EffectiveDiscount / 100m);

        foreach (var item in cartItems)
        {
            if (item.Product.CurrentStock < item.Quantity)
                return BadRequest($"Товар \"{item.Product.Name}\" в количестве {item.Quantity} шт. отсутствует на складе.");
        }

        var validMethods = new[] { "Card", "Cash", "Credit" };
        if (!validMethods.Contains(request.PaymentMethod))
            return BadRequest("Неверный способ оплаты");

        var orderNumber = await GenerateOrderNumberAsync();

        // Списание остатков
        foreach (var item in cartItems)
            item.Product.CurrentStock -= item.Quantity;

        var orderItems = cartItems.Select(ci => new OrderItem
        {
            ProductId = ci.ProductId,
            Quantity = ci.Quantity,
            PriceAtSale = ci.Product.SalePrice * discountFactor
        }).ToList();

        decimal total = orderItems.Sum(oi => oi.Quantity * oi.PriceAtSale);

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId.Value,
            OrderDate = DateTime.UtcNow,
            TotalAmount = total,
            Status = "Pending",                // стандартное ожидание
            PaymentMethod = request.PaymentMethod,
            OrderItems = orderItems
        };

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        // После создания заказа нет нужды обновлять лояльность,
        // так как статус Pending не влияет на неё.

        return new JsonResult(new { success = true, orderId = order.Id });
    }

    private int? GetCustomerId()
    {
        var claim = User.FindFirst("CustomerId")?.Value;
        return int.TryParse(claim, out int id) ? id : null;
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

    /// <summary>Быстрое обновление лояльности (один SQL‑запрос).</summary>
    private async Task UpdateLoyaltyDiscountFast(Customer customer)
    {
        // Один запрос к БД вместо загрузки всех сущностей
        int count = await _context.Orders
            .Where(o => o.CustomerId == customer.Id && o.Status == "Completed")
            .CountAsync();

        decimal sum = await _context.Orders
            .Where(o => o.CustomerId == customer.Id && o.Status == "Completed")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        customer.TotalCompletedOrders = count;
        customer.TotalSpent = sum;
        customer.LoyaltyDiscount = (count, sum) switch
        {
            ( >= 20, >= 200000) => 7m,
            ( >= 10, >= 50000) => 5m,
            ( >= 5, >= 10000) => 3m,
            _ => 0m
        };
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
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