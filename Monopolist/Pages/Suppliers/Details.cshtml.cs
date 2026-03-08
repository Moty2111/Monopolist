using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;

namespace Monoplist.Pages.Suppliers;

[Authorize(Roles = "Admin,Manager,Seller")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public SupplierDetailsViewModel Supplier { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var supplier = await _context.Suppliers
            .Include(s => s.Products)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
            return NotFound();

        Supplier = new SupplierDetailsViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactInfo = supplier.ContactInfo,
            Products = supplier.Products.Select(p => new SupplierProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                CategoryName = p.Category?.Name ?? "Без категории",
                Price = p.SalePrice,
                CurrentStock = p.CurrentStock
            }).ToList()
        };

        return Page();
    }
}