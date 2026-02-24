using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monopolist.ViewModels.Order;

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

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        Order.Id = order.Id;
        Order.CustomerId = order.CustomerId;
        Order.TotalAmount = order.TotalAmount;
        Order.Status = order.Status;
        Order.PaymentMethod = order.PaymentMethod;

        await PopulateDropdownsAsync();
        return Page();
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
            var order = await _context.Orders.FindAsync(Order.Id);
            if (order == null)
                return NotFound();

            order.CustomerId = Order.CustomerId;
            order.TotalAmount = Order.TotalAmount;
            order.Status = Order.Status;
            order.PaymentMethod = Order.PaymentMethod;
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Заказ обновлён.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Orders.AnyAsync(o => o.Id == Order.Id))
                return NotFound();
            else
                throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении заказа {OrderId}", Order.Id);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении.");
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
}