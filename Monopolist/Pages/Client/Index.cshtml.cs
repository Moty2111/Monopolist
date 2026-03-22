using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<ProductCardViewModel> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string CustomerName { get; set; } = string.Empty;
    public decimal CustomerDiscount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }
    [BindProperty(SupportsGet = true)]
    public int CategoryId { get; set; }

    public int SelectedCategoryId => CategoryId;

    public async Task OnGetAsync()
    {
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (customerIdClaim != null && int.TryParse(customerIdClaim, out int customerId))
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer != null)
            {
                CustomerName = customer.FullName;
                CustomerDiscount = customer.Discount;
            }
        }

        Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Warehouse)
            .Where(p => p.CurrentStock > 0)
            .AsQueryable();

        if (!string.IsNullOrEmpty(Search))
            query = query.Where(p => p.Name.Contains(Search) || (p.Article != null && p.Article.Contains(Search)));

        if (CategoryId > 0)
            query = query.Where(p => p.CategoryId == CategoryId);

        query = SortBy switch
        {
            "PriceAsc" => query.OrderBy(p => p.SalePrice),
            "PriceDesc" => query.OrderByDescending(p => p.SalePrice),
            "StockAsc" => query.OrderBy(p => p.CurrentStock),
            "StockDesc" => query.OrderByDescending(p => p.CurrentStock),
            _ => query.OrderBy(p => p.Name)
        };

        Products = await query
            .Select(p => new ProductCardViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Article = p.Article,
                CategoryName = p.Category != null ? p.Category.Name : "Без категории",
                Unit = p.Unit,
                SalePrice = p.SalePrice,
                CurrentStock = p.CurrentStock,
                ImageUrl = p.ImageUrl ?? (p.Warehouse != null ? p.Warehouse.ImageUrl : null),
                Description = p.Warehouse != null ? p.Warehouse.Description : null
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity)
    {
        TempData["Info"] = "Товар добавлен в корзину (демо)";
        return RedirectToPage();
    }
}

public class ProductCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = "шт";
    public decimal SalePrice { get; set; }
    public int CurrentStock { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
}