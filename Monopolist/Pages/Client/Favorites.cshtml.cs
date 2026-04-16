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
[IgnoreAntiforgeryToken]
public class FavoritesModel : PageModel
{
    private readonly AppDbContext _context;

    public FavoritesModel(AppDbContext context)
    {
        _context = context;
    }

    public List<ProductCardViewModel> FavoriteProducts { get; set; } = new();
    public string CustomerName { get; set; } = "Гость";
    public string? AvatarUrl { get; set; }
    public decimal CustomerDiscount { get; set; }
    public bool IsGuest { get; private set; }

    public async Task OnGetAsync()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        IsGuest = role != "Customer";

        if (!IsGuest)
        {
            var customerId = GetCustomerId();
            if (customerId != null && customerId > 0)
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer != null)
                {
                    CustomerName = customer.FullName;
                    CustomerDiscount = customer.Discount;
                    AvatarUrl = customer.AvatarUrl;
                }

                var favorites = await _context.Favorites
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Warehouse)
                    .Where(f => f.CustomerId == customerId)
                    .Select(f => f.Product)
                    .ToListAsync();

                FavoriteProducts = favorites.Select(p => new ProductCardViewModel
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
                }).ToList();
            }
            else
            {
                IsGuest = true;
                CustomerName = "Гость";
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> OnPostAddAsync(int productId)
    {
        if (IsGuest) return Unauthorized();
        var customerId = GetCustomerId();
        if (customerId == null || customerId <= 0) return Unauthorized();

        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == customerId && f.ProductId == productId);

        if (existing == null)
        {
            _context.Favorites.Add(new Favorite
            {
                CustomerId = customerId.Value,
                ProductId = productId
            });
            await _context.SaveChangesAsync();
        }
        return new JsonResult(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> OnPostRemoveAsync(int productId)
    {
        if (IsGuest) return Unauthorized();
        var customerId = GetCustomerId();
        if (customerId == null || customerId <= 0) return Unauthorized();

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == customerId && f.ProductId == productId);

        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }
        return new JsonResult(new { success = true });
    }

    private int? GetCustomerId()
    {
        var claim = User.FindFirst("CustomerId")?.Value;
        if (claim != null && int.TryParse(claim, out int id) && id > 0) return id;
        return null;
    }
}