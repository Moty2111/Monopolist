using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Monoplist.Data;
using Monoplist.Models;

namespace Monoplist.Pages.Warehouse
{
    [Authorize(Roles = "Admin,Manager")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.Warehouse Warehouse { get; set; } = new();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Warehouses.Add(Warehouse);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}