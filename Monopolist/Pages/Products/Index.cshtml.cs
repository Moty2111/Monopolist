using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;
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

    // Параметры сортировки
    [BindProperty(SupportsGet = true)]
    public string? SortField { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortOrder { get; set; } // "asc" или "desc"

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

            // Устанавливаем значения по умолчанию для сортировки
            SortField = string.IsNullOrEmpty(SortField) ? "Name" : SortField;
            SortOrder = string.IsNullOrEmpty(SortOrder) ? "asc" : SortOrder;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            // Фильтрация по поисковому запросу
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(p =>
                    EF.Functions.Like(p.Name, $"%{SearchString}%") ||
                    (p.Article != null && EF.Functions.Like(p.Article, $"%{SearchString}%")));
            }

            // Сортировка в зависимости от выбранного поля и направления
            IQueryable<Product> sortedQuery = SortField switch
            {
                "Article" => SortOrder == "asc"
                    ? query.OrderBy(p => p.Article)
                    : query.OrderByDescending(p => p.Article),
                "PurchasePrice" => SortOrder == "asc"
                    ? query.OrderBy(p => p.PurchasePrice)
                    : query.OrderByDescending(p => p.PurchasePrice),
                "SalePrice" => SortOrder == "asc"
                    ? query.OrderBy(p => p.SalePrice)
                    : query.OrderByDescending(p => p.SalePrice),
                "CurrentStock" => SortOrder == "asc"
                    ? query.OrderBy(p => p.CurrentStock)
                    : query.OrderByDescending(p => p.CurrentStock),
                "CategoryName" => SortOrder == "asc"
                    ? query.OrderBy(p => p.Category.Name)
                    : query.OrderByDescending(p => p.Category.Name),
                "SupplierName" => SortOrder == "asc"
                    ? query.OrderBy(p => p.Supplier.Name)
                    : query.OrderByDescending(p => p.Supplier.Name),
                _ => SortOrder == "asc"
                    ? query.OrderBy(p => p.Name)
                    : query.OrderByDescending(p => p.Name)
            };

            Products = await sortedQuery
                .Select(p => new ProductIndexViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Article = p.Article,
                    CategoryName = p.Category != null ? p.Category.Name : GetLocalizedMessage("Без категории", "Uncategorized", "Санатсыз"),
                    Unit = p.Unit,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice,
                    CurrentStock = p.CurrentStock,
                    SupplierName = p.Supplier != null ? p.Supplier.Name : GetLocalizedMessage("Не указан", "Not specified", "Көрсетілмеген")
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