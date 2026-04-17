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
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(AppDbContext context, ILogger<CreateModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public OrderCreateViewModel Order { get; set; } = new();

    public SelectList Customers { get; set; } = default!;
    public List<SelectListItem> Statuses { get; set; } = new();
    public List<SelectListItem> PaymentMethods { get; set; } = new();

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();
        await PopulateDropdownsAsync();
        await LoadAvailableProducts();
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
            var orderNumber = await GenerateOrderNumberAsync();
            var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var currentUserName = User.Identity?.Name ?? "Пользователь";

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = Order.CustomerId,
                OrderDate = DateTime.Now,
                TotalAmount = Order.Items.Sum(i => i.Quantity * i.Price),
                Status = Order.Status ?? "Pending",
                PaymentMethod = Order.PaymentMethod,
                OrderItems = new List<OrderItem>()
            };

            // Получаем информацию о клиенте для уведомления
            var customer = await _context.Customers.FindAsync(Order.CustomerId);
            var customerName = customer?.FullName ?? "Клиент";

            foreach (var item in Order.Items)
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

                // Уменьшаем остаток товара
                product.CurrentStock -= item.Quantity;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // === УВЕДОМЛЕНИЯ ===
            // 1. Администраторам о новом заказе
            await NotificationService.CreateForAdminsAsync(_context,
                GetLocalizedMessage("Новый заказ", "New order", "Жаңа тапсырыс"),
                GetLocalizedMessage(
                    $"Заказ №{orderNumber} от {customerName} на сумму {order.TotalAmount:N0} ₽",
                    $"Order #{orderNumber} from {customerName} for {order.TotalAmount:N0} ₽",
                    $"{orderNumber} тапсырысы {customerName}-дан {order.TotalAmount:N0} ₽ сомасына"),
                NotificationType.Order,
                $"/Orders/Details?id={order.Id}");

            // 2. Самому создателю заказа (текущему менеджеру/админу) – опционально
            await NotificationService.CreateForUserAsync(_context, currentUserId,
                GetLocalizedMessage("Заказ создан", "Order created", "Тапсырыс құрылды"),
                GetLocalizedMessage(
                    $"Вы успешно создали заказ №{orderNumber} для {customerName}.",
                    $"You successfully created order #{orderNumber} for {customerName}.",
                    $"Сіз {customerName} үшін {orderNumber} тапсырысын сәтті құрдыңыз."),
                NotificationType.Success,
                $"/Orders/Details?id={order.Id}");

            // 3. Если статус "Completed" – уведомление клиенту (если есть CustomerId и система клиентских уведомлений)
            //    Это можно добавить позже, если есть отдельная таблица клиентов и их уведомлений.

            TempData["Success"] = GetLocalizedMessage(
                $"Заказ {orderNumber} успешно создан.",
                $"Order {orderNumber} created successfully.",
                $"{orderNumber} тапсырысы сәтті құрылды.");

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании заказа");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при сохранении. Попробуйте снова.",
                "An error occurred while saving. Please try again.",
                "Сақтау кезінде қате орын алды. Қайталап көріңіз."));
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

    private async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = $"ORD-{today:yyyyMMdd}-";

        var lastOrderToday = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastOrderToday))
        {
            var lastNumberStr = lastOrderToday[prefix.Length..];
            if (int.TryParse(lastNumberStr, out int lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"{prefix}{nextNumber:D3}";
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