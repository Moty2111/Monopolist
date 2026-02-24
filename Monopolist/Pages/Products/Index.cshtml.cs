using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;

namespace Monoplist.Pages.Products;

[Authorize(Roles = "Admin,Manager,Seller")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<ProductIndexViewModel> Products { get; set; } = new List<ProductIndexViewModel>();

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    public async Task OnGetAsync()
    {
        // Устанавливаем заголовок страницы для отображения в представлении
        ViewData["Title"] = "Каталог товаров";

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{SearchString}%") ||
                EF.Functions.Like(p.Article ?? "", $"%{SearchString}%"));
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
}