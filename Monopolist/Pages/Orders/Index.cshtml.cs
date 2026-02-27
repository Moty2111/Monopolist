using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;

namespace Monoplist.Pages.Orders;

[Authorize(Roles = "Admin,Manager,Seller")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    public IList<OrderIndexViewModel> Orders { get; set; } = new List<OrderIndexViewModel>();

    public async Task OnGetAsync()
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .AsQueryable();

        // Применяем фильтр поиска, если задан SearchString
        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(o =>
                EF.Functions.Like(o.OrderNumber, $"%{SearchString}%") ||
                (o.Customer != null && EF.Functions.Like(o.Customer.FullName, $"%{SearchString}%"))
            );
        }

        Orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderIndexViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer != null ? o.Customer.FullName : "Неизвестно",
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod
            })
            .ToListAsync();
    }
}