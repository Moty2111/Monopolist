// Pages/Orders/Edit.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using Monopolist.ViewModels.Order;
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

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        await LoadUserSettings();

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
            await LoadUserSettings();
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

            TempData["Success"] = GetLocalizedMessage("Заказ обновлён.", "Order updated.", "Тапсырыс жаңартылды.");
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
            ModelState.AddModelError(string.Empty, GetLocalizedMessage("Произошла ошибка при обновлении.", "An error occurred while updating.", "Жаңарту кезінде қате орын алды."));
            await LoadUserSettings();
            await PopulateDropdownsAsync();
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