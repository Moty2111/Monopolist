using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;

namespace Monoplist.Pages.Warehouse
{
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

        public IList<Models.Warehouse> Warehouses { get; set; } = new List<Models.Warehouse>();

        public async Task OnGetAsync()
        {
            var query = _context.Warehouses.AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(w =>
                    EF.Functions.Like(w.Name, $"%{SearchString}%") ||
                    (w.Location != null && EF.Functions.Like(w.Location, $"%{SearchString}%")));
            }

            Warehouses = await query.OrderBy(w => w.Name).ToListAsync();
        }
    }
}