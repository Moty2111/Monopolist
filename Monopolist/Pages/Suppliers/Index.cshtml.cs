using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using Monopolist.ViewModels.Supplier;

namespace Monoplist.Pages.Suppliers;

[Authorize(Roles = "Admin,Manager,Seller")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<SupplierIndexViewModel> Suppliers { get; set; } = new List<SupplierIndexViewModel>();

    public async Task OnGetAsync()
    {
        Suppliers = await _context.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SupplierIndexViewModel
            {
                Id = s.Id,
                Name = s.Name,
                ContactInfo = s.ContactInfo,
                ProductsCount = s.Products.Count
            })
            .ToListAsync();
    }
}