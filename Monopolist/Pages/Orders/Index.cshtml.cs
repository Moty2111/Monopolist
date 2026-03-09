// Pages/Orders/Index.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Orders;

[Authorize(Roles = "Admin,Manager,Seller")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    public IList<OrderIndexViewModel> Orders { get; set; } = new List<OrderIndexViewModel>();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        try
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

            var query = _context.Orders
                .Include(o => o.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(o =>
                    EF.Functions.Like(o.OrderNumber, $"%{SearchString}%") ||
                    (o.Customer != null && EF.Functions.Like(o.Customer.FullName, $"%{SearchString}%")));
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка заказов");
            TempData["Error"] = "Не удалось загрузить список заказов.";
        }
    }
}