using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Warehouse;

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

    public IList<WarehouseViewModel> Warehouses { get; set; } = new List<WarehouseViewModel>();

    public async Task OnGetAsync()
    {
        try
        {
            var query = _context.Warehouses
                .Include(w => w.Products)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(w =>
                    EF.Functions.Like(w.Name, $"%{SearchString}%") ||
                    (w.Location != null && EF.Functions.Like(w.Location, $"%{SearchString}%")));
            }

            var warehouses = await query.OrderBy(w => w.Name).ToListAsync();

            Warehouses = warehouses.Select(w => new WarehouseViewModel
            {
                Id = w.Id,
                Name = w.Name,
                Location = w.Location,
                ImageUrl = w.ImageUrl,
                Description = w.Description,
                Capacity = w.Capacity,
                CurrentOccupancy = w.Products.Sum(p => p.CurrentStock), // Products не null после Include
                ProductsCount = w.Products.Count,
                ProductNames = w.Products.Select(p => p.Name).Take(3).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке складов");
            TempData["Error"] = "Не удалось загрузить список складов.";
        }
    }
}