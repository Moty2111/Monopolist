using Microsoft.AspNetCore.Authorization;
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

    public IList<OrderIndexViewModel> Orders { get; set; } = new List<OrderIndexViewModel>();

    public async Task OnGetAsync()
    {
        Orders = await _context.Orders
            .Include(o => o.Customer)
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