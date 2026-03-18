using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Suppliers;

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

    // Параметры сортировки
    [BindProperty(SupportsGet = true)]
    public string? SortField { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortOrder { get; set; }

    public IList<SupplierIndexViewModel> Suppliers { get; set; } = new List<SupplierIndexViewModel>();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();

        // Устанавливаем значения по умолчанию для сортировки
        SortField = string.IsNullOrEmpty(SortField) ? "Name" : SortField;
        SortOrder = string.IsNullOrEmpty(SortOrder) ? "asc" : SortOrder;

        var query = _context.Suppliers
            .Include(s => s.Products)
            .AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(s =>
                EF.Functions.Like(s.Name, $"%{SearchString}%") ||
                EF.Functions.Like(s.ContactInfo, $"%{SearchString}%"));
        }

        // Сортировка
        query = SortField switch
        {
            "ContactInfo" => SortOrder == "asc"
                ? query.OrderBy(s => s.ContactInfo)
                : query.OrderByDescending(s => s.ContactInfo),
            "ProductsCount" => SortOrder == "asc"
                ? query.OrderBy(s => s.Products.Count)
                : query.OrderByDescending(s => s.Products.Count),
            _ => SortOrder == "asc"
                ? query.OrderBy(s => s.Name)
                : query.OrderByDescending(s => s.Name)
        };

        Suppliers = await query
            .Select(s => new SupplierIndexViewModel
            {
                Id = s.Id,
                Name = s.Name,
                ContactInfo = s.ContactInfo,
                ProductsCount = s.Products.Count
            })
            .ToListAsync();
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
}