using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie, GuestCookie")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<ProductCardViewModel> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string CustomerName { get; set; } = "Гость";
    public string? AvatarUrl { get; set; }
    public decimal CustomerDiscount { get; set; }
    public bool IsGuest { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; } = "Name";

    [BindProperty(SupportsGet = true)]
    public int CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 12;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int SelectedCategoryId => CategoryId;

    public async Task OnGetAsync()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        IsGuest = role != "Customer";

        if (!IsGuest && int.TryParse(customerIdClaim, out int customerId))
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer != null)
            {
                CustomerName = customer.FullName;
                CustomerDiscount = customer.Discount;
                AvatarUrl = customer.AvatarUrl;
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

        TotalItems = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

        query = SortBy switch
        {
            "PriceAsc" => query.OrderBy(p => p.SalePrice),
            "PriceDesc" => query.OrderByDescending(p => p.SalePrice),
            "StockAsc" => query.OrderBy(p => p.CurrentStock),
            "StockDesc" => query.OrderByDescending(p => p.CurrentStock),
            _ => query.OrderBy(p => p.Name)
        };

        var discountFactor = 1 - (CustomerDiscount / 100m);

        Products = await query
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new ProductCardViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Article = p.Article,
                CategoryName = p.Category != null ? p.Category.Name : "Без категории",
                Unit = p.Unit,
                SalePrice = p.SalePrice,
                DiscountedPrice = p.SalePrice * discountFactor,
                CurrentStock = p.CurrentStock,
                ImageUrl = p.ImageUrl ?? (p.Warehouse != null ? p.Warehouse.ImageUrl : null),
                Description = p.Warehouse != null ? p.Warehouse.Description : null
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetFavorites()
    {
        if (IsGuest) return new JsonResult(new List<int>());
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (!int.TryParse(customerIdClaim, out int customerId)) return new JsonResult(new List<int>());

        var favorites = await _context.Favorites
            .Where(f => f.CustomerId == customerId)
            .Select(f => f.ProductId)
            .ToListAsync();
        return new JsonResult(favorites);
    }
}