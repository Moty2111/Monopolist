using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

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

    // Параметры сортировки
    [BindProperty(SupportsGet = true)]
    public string? SortField { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortOrder { get; set; }

    public IList<WarehouseViewModel> Warehouses { get; set; } = new List<WarehouseViewModel>();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();

        try
        {
            // Устанавливаем значения по умолчанию для сортировки
            SortField = string.IsNullOrEmpty(SortField) ? "Name" : SortField;
            SortOrder = string.IsNullOrEmpty(SortOrder) ? "asc" : SortOrder;

            var query = _context.Warehouses
                .Include(w => w.Products)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(w =>
                    EF.Functions.Like(w.Name, $"%{SearchString}%") ||
                    (w.Location != null && EF.Functions.Like(w.Location, $"%{SearchString}%")));
            }

            // Сортировка
            query = SortField switch
            {
                "Location" => SortOrder == "asc"
                    ? query.OrderBy(w => w.Location)
                    : query.OrderByDescending(w => w.Location),
                "Capacity" => SortOrder == "asc"
                    ? query.OrderBy(w => w.Capacity)
                    : query.OrderByDescending(w => w.Capacity),
                "CurrentOccupancy" => SortOrder == "asc"
                    ? query.OrderBy(w => w.CurrentOccupancy)
                    : query.OrderByDescending(w => w.CurrentOccupancy),
                "ProductsCount" => SortOrder == "asc"
                    ? query.OrderBy(w => w.Products.Count)
                    : query.OrderByDescending(w => w.Products.Count),
                _ => SortOrder == "asc"
                    ? query.OrderBy(w => w.Name)
                    : query.OrderByDescending(w => w.Name)
            };

            var warehouses = await query.ToListAsync();

            Warehouses = warehouses.Select(w => new WarehouseViewModel
            {
                Id = w.Id,
                Name = w.Name,
                Location = w.Location,
                ImageUrl = w.ImageUrl,
                Description = w.Description,
                Capacity = w.Capacity,
                CurrentOccupancy = w.Products.Sum(p => p.CurrentStock),
                ProductsCount = w.Products.Count,
                ProductNames = w.Products.Select(p => p.Name).Take(3).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке складов");
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось загрузить список складов.",
                "Failed to load warehouse list.",
                "Қоймалар тізімін жүктеу мүмкін болмады.");
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