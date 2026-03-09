using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Orders;

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

    public OrderDeleteViewModel Order { get; set; } = new();

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

        var order = await _context.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        Order = new OrderDeleteViewModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.Customer?.FullName ?? "Неизвестно",
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            TempData["Error"] = GetLocalizedMessage("Заказ не найден.", "Order not found.", "Тапсырыс табылмады.");
            return RedirectToPage("./Index");
        }

        try
        {
            if (order.OrderItems != null && order.OrderItems.Any())
            {
                _context.OrderItems.RemoveRange(order.OrderItems);
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage($"Заказ {order.OrderNumber} удалён.", $"Order {order.OrderNumber} deleted.", $"{order.OrderNumber} тапсырысы жойылды.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении заказа {OrderId}", id);
            TempData["Error"] = GetLocalizedMessage("Не удалось удалить заказ.", "Failed to delete order.", "Тапсырысты жою мүмкін болмады.");
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