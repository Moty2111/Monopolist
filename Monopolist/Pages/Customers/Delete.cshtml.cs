using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Customers;

[Authorize(Roles = "Admin,Manager")]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(AppDbContext context, ILogger<DeleteModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Customer Customer { get; set; } = new();

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

        Customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id);

        if (Customer == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            TempData["Error"] = GetLocalizedMessage("Клиент не найден.", "Customer not found.", "Клиент табылмады.");
            return RedirectToPage("./Index");
        }

        // Проверка на наличие заказов
        bool hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == id);
        if (hasOrders)
        {
            TempData["Error"] = GetLocalizedMessage("Нельзя удалить клиента, у которого есть заказы. Сначала удалите заказы.", "Cannot delete a customer with orders. Delete the orders first.", "Тапсырыстары бар клиентті жою мүмкін емес. Алдымен тапсырыстарды жойыңыз.");
            return RedirectToPage("./Index");
        }

        try
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage("Клиент удалён.", "Customer deleted.", "Клиент жойылды.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении клиента");
            TempData["Error"] = GetLocalizedMessage("Не удалось удалить клиента.", "Failed to delete customer.", "Клиентті жою мүмкін болмады.");
        }

        return RedirectToPage("./Index");
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