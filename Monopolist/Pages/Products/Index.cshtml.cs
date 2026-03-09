using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using System.Security.Claims;

namespace Monoplist.Pages.Products;

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

    public IList<ProductIndexViewModel> Products { get; set; } = new List<ProductIndexViewModel>();

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

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
            await LoadUserSettings();

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(p =>
                    EF.Functions.Like(p.Name, $"%{SearchString}%") ||
                    (p.Article != null && EF.Functions.Like(p.Article, $"%{SearchString}%")));
            }

            Products = await query
                .Select(p => new ProductIndexViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Article = p.Article,
                    CategoryName = p.Category != null ? p.Category.Name : "Без категории",
                    Unit = p.Unit,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice,
                    CurrentStock = p.CurrentStock,
                    SupplierName = p.Supplier != null ? p.Supplier.Name : "Не указан"
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка товаров");
            TempData["Error"] = GetLocalizedMessage("Не удалось загрузить список товаров.", "Failed to load products.", "Тауарлар тізімін жүктеу мүмкін болмады.");
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