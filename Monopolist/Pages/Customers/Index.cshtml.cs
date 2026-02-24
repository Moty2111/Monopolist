using Microsoft.AspNetCore.Authorization;
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

    public IList<Customer> Customers { get; set; } = new List<Customer>();

    public async Task OnGetAsync()
    {
        Customers = await _context.Customers
            .OrderBy(c => c.FullName)
            .ToListAsync();
    }
}