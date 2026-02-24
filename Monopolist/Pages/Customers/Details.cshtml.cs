using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;

namespace Monoplist.Pages.Customers;

[Authorize(Roles = "Admin,Manager,Seller")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Customer Customer { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        Customer = await _context.Customers
            .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (Customer == null)
            return NotFound();

        return Page();
    }
}