using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.Services;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Orders;

[Authorize(Roles = "Admin,Manager")]
public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<EditModel> _logger;

    public EditModel(AppDbContext context, ILogger<EditModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public OrderEditViewModel Order { get; set; } = new();

    public SelectList Customers { get; set; } = default!;
    public List<SelectListItem> Statuses { get; set; } = new();
    public List<SelectListItem> PaymentMethods { get; set; } = new();

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();
        await LoadUserSettings();

        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        Order.Id = order.Id;
        Order.CustomerId = order.CustomerId;
        Order.TotalAmount = order.TotalAmount;
        Order.Status = order.Status;
        Order.PaymentMethod = order.PaymentMethod;
        Order.Items = order.OrderItems.Select(oi => new OrderItemViewModel
        {
            ProductId = oi.ProductId,
            ProductName = oi.Product?.Name ?? "Неизвестно",
            Quantity = oi.Quantity,
            Price = oi.PriceAtSale,
            Unit = oi.Product?.Unit ?? "шт"
        }).ToList();

        await PopulateDropdownsAsync();
        await LoadAvailableProducts();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserSettings();
            await PopulateDropdownsAsync();
            await LoadAvailableProducts();
            return Page();
        }

        Order.Items = Order.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();

        if (!Order.Items.Any())
        {
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Добавьте хотя бы один товар в заказ.",
                "Add at least one product to the order.",
                "Кемінде бір тауар қосыңыз."));
            await LoadUserSettings();
            await PopulateDropdownsAsync();
            await LoadAvailableProducts();
            return Page();
        }

        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == Order.Id);

            if (order == null) return NotFound();

            var oldStatus = order.Status;
            var newStatus = Order.Status;
            var customer = await _context.Customers.FindAsync(order.CustomerId);
            var customerName = customer?.FullName ?? "Клиент";

            // Обновляем основные поля
            order.CustomerId = Order.CustomerId;
            order.Status = newStatus;
            order.PaymentMethod = Order.PaymentMethod;
            order.UpdatedAt = DateTime.Now;

            // Обновляем позиции
            var existingItems = order.OrderItems.ToList();
            var newItems = Order.Items.Where(i => !existingItems.Any(e => e.ProductId == i.ProductId)).ToList();
            var removedItems = existingItems.Where(e => !Order.Items.Any(i => i.ProductId == e.ProductId)).ToList();
            var updatedItems = existingItems.Where(e => Order.Items.Any(i => i.ProductId == e.ProductId)).ToList();

            // Удаляем отсутствующие
            foreach (var item in removedItems)
            {
                _context.OrderItems.Remove(item);
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.CurrentStock += item.Quantity;
                }
            }

            // Добавляем новые
            foreach (var item in newItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                        $"Товар с ID {item.ProductId} не найден.",
                        $"Product with ID {item.ProductId} not found.",
                        $"{item.ProductId} ID тауар табылмады."));
                    await LoadUserSettings();
                    await PopulateDropdownsAsync();
                    await LoadAvailableProducts();
                    return Page();
                }

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PriceAtSale = product.SalePrice
                });

                product.CurrentStock -= item.Quantity;
            }

            // Обновляем существующие
            foreach (var item in updatedItems)
            {
                var newItem = Order.Items.First(i => i.ProductId == item.ProductId);
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null) continue;

                var delta = newItem.Quantity - item.Quantity;
                product.CurrentStock -= delta;

                item.Quantity = newItem.Quantity;
                item.PriceAtSale = product.SalePrice;
            }

            order.TotalAmount = Order.Items.Sum(i => i.Quantity * i.Price);
            await _context.SaveChangesAsync();

            // === УВЕДОМЛЕНИЯ ===
            // 1. Если статус изменился на "Completed"
            if (oldStatus != "Completed" && newStatus == "Completed")
            {
                await NotificationService.CreateForAdminsAsync(_context,
                    GetLocalizedMessage("Заказ выполнен", "Order completed", "Тапсырыс орындалды"),
                    GetLocalizedMessage(
                        $"Заказ №{order.OrderNumber} для {customerName} выполнен. Сумма: {order.TotalAmount:N0} ₽",
                        $"Order #{order.OrderNumber} for {customerName} completed. Amount: {order.TotalAmount:N0} ₽",
                        $"{order.OrderNumber} тапсырысы {customerName} үшін орындалды. Сомасы: {order.TotalAmount:N0} ₽"),
                    NotificationType.Success,
                    $"/Orders/Details?id={order.Id}");
            }
            // Если статус изменился на "Cancelled"
            else if (oldStatus != "Cancelled" && newStatus == "Cancelled")
            {
                await NotificationService.CreateForAdminsAsync(_context,
                    GetLocalizedMessage("Заказ отменён", "Order cancelled", "Тапсырыс бас тартылды"),
                    GetLocalizedMessage(
                        $"Заказ №{order.OrderNumber} для {customerName} был отменён.",
                        $"Order #{order.OrderNumber} for {customerName} was cancelled.",
                        $"{order.OrderNumber} тапсырысы {customerName} үшін бас тартылды."),
                    NotificationType.Warning,
                    $"/Orders/Details?id={order.Id}");
            }

            TempData["Success"] = GetLocalizedMessage(
                $"Заказ {order.OrderNumber} обновлён.",
                $"Order {order.OrderNumber} updated.",
                $"{order.OrderNumber} тапсырысы жаңартылды.");

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении заказа {OrderId}", Order.Id);
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при обновлении.",
                "An error occurred while updating.",
                "Жаңарту кезінде қате орын алды."));
            await LoadUserSettings();
            await PopulateDropdownsAsync();
            await LoadAvailableProducts();
            return Page();
        }
    }

    private async Task PopulateDropdownsAsync()
    {
        var customers = await _context.Customers
            .OrderBy(c => c.FullName)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.FullName
            })
            .ToListAsync();
        Customers = new SelectList(customers, "Value", "Text");

        Statuses = new List<SelectListItem>
        {
            new() { Value = "Pending", Text = Language == "ru" ? "Ожидание" : Language == "en" ? "Pending" : "Күтілуде" },
            new() { Value = "Processing", Text = Language == "ru" ? "В обработке" : Language == "en" ? "Processing" : "Өңделуде" },
            new() { Value = "Completed", Text = Language == "ru" ? "Завершён" : Language == "en" ? "Completed" : "Аяқталды" },
            new() { Value = "Cancelled", Text = Language == "ru" ? "Отменён" : Language == "en" ? "Cancelled" : "Бас тартылды" }
        };

        PaymentMethods = new List<SelectListItem>
        {
            new() { Value = "Cash", Text = Language == "ru" ? "Наличные" : Language == "en" ? "Cash" : "Қолма-қол" },
            new() { Value = "Card", Text = Language == "ru" ? "Карта" : Language == "en" ? "Card" : "Карта" },
            new() { Value = "Credit", Text = Language == "ru" ? "Кредит/Рассрочка" : Language == "en" ? "Credit" : "Несие" }
        };
    }

    private async Task LoadAvailableProducts()
    {
        Order.AvailableProducts = await _context.Products
            .Where(p => p.CurrentStock > 0)
            .OrderBy(p => p.Name)
            .Select(p => new SelectItem
            {
                Value = p.Id,
                Text = $"{p.Name} (в наличии: {p.CurrentStock} {p.Unit})",
                Price = p.SalePrice,
                Unit = p.Unit
            })
            .ToListAsync();
    }

    private async Task LoadUserSettings()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            Language = user.Language ?? "ru";
            CompactMode = user.CompactMode;
            Animations = user.Animations;
            Theme = user.Theme ?? "light";
            CustomColor = user.CustomColor ?? "#FF6B00";
        }
    }

    private string GetLocalizedMessage(string ru, string en, string kk)
    {
        return Language switch
        {
            "en" => en,
            "kk" => kk,
            _ => ru
        };
    }
}