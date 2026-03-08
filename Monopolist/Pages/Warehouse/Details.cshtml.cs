using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Warehouse;

[Authorize(Roles = "Admin,Manager,Seller")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(AppDbContext context, ILogger<DetailsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public WarehouseDetailViewModel Warehouse { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.Products)
                    .ThenInclude(p => p.Category)
                .Include(w => w.Products)
                    .ThenInclude(p => p.Supplier)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (warehouse == null)
            {
                return NotFound();
            }

            Warehouse = new WarehouseDetailViewModel
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Location = warehouse.Location,
                ImageUrl = warehouse.ImageUrl,
                Description = warehouse.Description,
                Capacity = warehouse.Capacity,
                CurrentOccupancy = warehouse.Products.Sum(p => p.CurrentStock),
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt,
                Products = warehouse.Products.Select(p => new ProductInfoViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Article = p.Article,
                    Category = p.Category != null ? p.Category.Name : "Без категории",
                    Supplier = p.Supplier != null ? p.Supplier.Name : "Без поставщика",
                    CurrentStock = p.CurrentStock,
                    Unit = p.Unit,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice
                }).OrderBy(p => p.Name).ToList()
            };

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке склада {WarehouseId}", id);
            TempData["Error"] = "Не удалось загрузить информацию о складе.";
            return RedirectToPage("./Index");
        }
    }
}