using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monopolist.ViewModels.Supplier;

namespace Monoplist.Pages.Suppliers;

[Authorize(Roles = "Admin,Manager")]
public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<EditModel> _logger;

    public EditModel(AppDbContext context, ILogger<EditModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public SupplierEditViewModel Supplier { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound();

        Supplier.Id = supplier.Id;
        Supplier.Name = supplier.Name;
        Supplier.ContactInfo = supplier.ContactInfo;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var supplier = await _context.Suppliers.FindAsync(Supplier.Id);
            if (supplier == null)
                return NotFound();

            supplier.Name = Supplier.Name;
            supplier.ContactInfo = Supplier.ContactInfo ?? string.Empty;
            supplier.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Поставщик «{supplier.Name}» обновлён.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Suppliers.AnyAsync(s => s.Id == Supplier.Id))
                return NotFound();
            else
                throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении поставщика {SupplierId}", Supplier.Id);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении.");
            return Page();
        }
    }
}