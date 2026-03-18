using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

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

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadUserSettings();

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
                    Category = p.Category != null ? p.Category.Name : GetLocalizedMessage("Без категории", "Uncategorized", "Санатсыз"),
                    Supplier = p.Supplier != null ? p.Supplier.Name : GetLocalizedMessage("Без поставщика", "No supplier", "Жеткізуші жоқ"),
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
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось загрузить информацию о складе.",
                "Failed to load warehouse information.",
                "Қойма туралы ақпаратты жүктеу мүмкін болмады.");
            return RedirectToPage("./Index");
        }
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