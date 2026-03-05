using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;

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

    public IList<SupplierIndexViewModel> Suppliers { get; set; } = new List<SupplierIndexViewModel>();

    public async Task OnGetAsync()
    {
        var query = _context.Suppliers
            .Include(s => s.Products)
            .AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(s =>
                EF.Functions.Like(s.Name, $"%{SearchString}%") ||
                EF.Functions.Like(s.ContactInfo, $"%{SearchString}%"));
        }

        Suppliers = await query
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