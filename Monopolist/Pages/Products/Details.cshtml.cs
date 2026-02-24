using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;

namespace Monoplist.Pages.Products;

[Authorize(Roles = "Admin,Manager,Seller")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public ProductDetailsViewModel Product { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        Product = new ProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Article = product.Article,
            CategoryName = product.Category?.Name ?? "Без категории",
            Unit = product.Unit,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            CurrentStock = product.CurrentStock,
            MinimumStock = product.MinimumStock,
            SupplierName = product.Supplier?.Name ?? "Не указан",
            SupplierContact = product.Supplier?.ContactInfo
        };

        return Page();
    }
}