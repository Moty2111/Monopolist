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
                    AvatarUrl = customer.AvatarUrl;

                    // Актуализируем лояльность перед показом корзины
                    await UpdateLoyaltyDiscount(customer);
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

        // Первая попытка – найти существующий элемент
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CustomerId == customerId && ci.ProductId == productId);

        if (cartItem != null)
        {
            int newQuantity = cartItem.Quantity + quantity;
            if (product.CurrentStock < newQuantity)
                return BadRequest("Недостаточно товара на складе");
            cartItem.Quantity = newQuantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        // Элемента нет – пробуем добавить с обработкой возможной гонки
        bool saved = false;
        while (!saved)
        {
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
                saved = true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
            {
                // Откатываем добавленную сущность (она конфликтует)
                var entry = _context.ChangeTracker.Entries<CartItem>()
                    .FirstOrDefault(e => e.Entity.CustomerId == customerId && e.Entity.ProductId == productId);
                if (entry != null)
                    entry.State = EntityState.Detached;

                // Загружаем реальную запись, созданную параллельным запросом
                cartItem = await _context.CartItems
                    .AsNoTracking()   // важно загрузить без отслеживания, чтобы избежать конфликтов
                    .FirstOrDefaultAsync(ci => ci.CustomerId == customerId && ci.ProductId == productId);

                if (cartItem != null)
                {
                    // Присоединяем к контексту и обновляем
                    _context.CartItems.Attach(cartItem);
                    int newQuantity = cartItem.Quantity + quantity;
                    if (product.CurrentStock < newQuantity)
                        return BadRequest("Недостаточно товара на складе");
                    cartItem.Quantity = newQuantity;
                    cartItem.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    saved = true;
                }
                else
                {
                    // На всякий случай, если запись всё ещё не появилась – повторяем попытку
                    // (не должно случиться, но защита от бесконечного цикла)
                    await Task.Delay(50);
                }
            }
            catch (Exception)
            {
                // Если другая ошибка – пробрасываем
                throw;
            }
        }

        return new JsonResult(new { success = true });
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

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return Unauthorized();

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.CustomerId == customerId)
            .ToListAsync();

        if (!cartItems.Any()) return BadRequest("Корзина пуста");

        // Обновляем скидку на основе завершённых заказов
        await UpdateLoyaltyDiscount(customer);
        var discountFactor = 1 - (customer.EffectiveDiscount / 100m);

        foreach (var item in cartItems)
        {
            if (item.Product.CurrentStock < item.Quantity)
                return BadRequest($"Товар \"{item.Product.Name}\" в количестве {item.Quantity} шт. отсутствует на складе. Доступно: {item.Product.CurrentStock}");
        }

        var validMethods = new[] { "Card", "Cash", "Credit" };
        if (!validMethods.Contains(request.PaymentMethod))
            return BadRequest("Неверный способ оплаты");

        var orderNumber = await GenerateOrderNumberAsync();

        // Списание остатков
        foreach (var item in cartItems)
        {
            item.Product.CurrentStock -= item.Quantity;
        }

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
            Status = "Pending",                // ← обычный статус ожидания
            PaymentMethod = request.PaymentMethod,
            OrderItems = orderItems
        };

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        // Лояльность не обновляем немедленно – она пересчитается при следующем входе в корзину/профиль,
        // когда администратор завершит заказ.

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

    private async Task UpdateLoyaltyDiscount(Customer customer)
    {
        var completedOrders = await _context.Orders
            .Where(o => o.CustomerId == customer.Id && o.Status == "Completed")
            .ToListAsync();

        int count = completedOrders.Count;
        decimal sum = completedOrders.Sum(o => o.TotalAmount);

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