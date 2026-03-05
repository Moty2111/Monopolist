using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;

namespace Monoplist.Pages.Customers;

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

    public IList<Customer> Customers { get; set; } = new List<Customer>();

    public async Task OnGetAsync()
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(c =>
                EF.Functions.Like(c.FullName, $"%{SearchString}%") ||
                EF.Functions.Like(c.Phone, $"%{SearchString}%") ||
                EF.Functions.Like(c.Email, $"%{SearchString}%"));
        }

        Customers = await query
            .OrderBy(c => c.FullName)
            .ToListAsync();
    }
}