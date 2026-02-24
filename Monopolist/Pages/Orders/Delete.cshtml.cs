using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;

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

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

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
            TempData["Error"] = "Заказ не найден.";
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

            TempData["Success"] = $"Заказ {order.OrderNumber} удалён.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении заказа {OrderId}", id);
            TempData["Error"] = "Не удалось удалить заказ.";
        }

        return RedirectToPage("./Index");
    }
}