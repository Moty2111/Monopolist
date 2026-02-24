using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;

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

    public async Task OnGetAsync()
    {
        await PopulateDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return Page();
        }

        try
        {
            var orderNumber = await GenerateOrderNumberAsync();

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = Order.CustomerId,
                OrderDate = DateTime.Now,
                TotalAmount = Order.TotalAmount,
                Status = Order.Status ?? "Pending",
                PaymentMethod = Order.PaymentMethod
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Заказ {orderNumber} успешно создан.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании заказа");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении. Попробуйте снова.");
            await PopulateDropdownsAsync();
            return Page();
        }
    }

    private async Task PopulateDropdownsAsync()
    {
        // Клиенты
        var customers = await _context.Customers
            .OrderBy(c => c.FullName)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.FullName
            })
            .ToListAsync();
        Customers = new SelectList(customers, "Value", "Text");

        // Статусы
        Statuses = new List<SelectListItem>
        {
            new() { Value = "Pending", Text = "Ожидание" },
            new() { Value = "Processing", Text = "В обработке" },
            new() { Value = "Completed", Text = "Завершён" },
            new() { Value = "Cancelled", Text = "Отменён" }
        };

        // Методы оплаты
        PaymentMethods = new List<SelectListItem>
        {
            new() { Value = "Cash", Text = "Наличные" },
            new() { Value = "Card", Text = "Карта" },
            new() { Value = "Credit", Text = "Кредит/Рассрочка" }
        };
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
}