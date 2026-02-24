using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;

namespace Monoplist.Pages.Orders;

[Authorize(Roles = "Admin,Manager,Seller")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public OrderDetailsViewModel Order { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        Order = new OrderDetailsViewModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.Customer?.FullName ?? "Неизвестно",
            CustomerPhone = order.Customer?.Phone,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            Items = order.OrderItems.Select(oi => new OrderItemViewModel
            {
                ProductName = oi.Product?.Name ?? "Неизвестно",
                Quantity = oi.Quantity,
                Price = oi.PriceAtSale,
                Total = oi.Quantity * oi.PriceAtSale
            }).ToList()
        };

        return Page();
    }
}