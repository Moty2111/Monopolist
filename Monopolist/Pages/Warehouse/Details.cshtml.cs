using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;

namespace Monoplist.Pages.Warehouse
{
    [Authorize(Roles = "Admin,Manager,Seller")]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Models.Warehouse Warehouse { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id);
            if (warehouse == null)
            {
                return NotFound();
            }
            Warehouse = warehouse;
            return Page();
        }
    }
}