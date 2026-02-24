using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;

namespace Monoplist.Pages.Suppliers;

[Authorize(Roles = "Admin,Manager")]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(AppDbContext context, ILogger<DeleteModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public SupplierDeleteViewModel Supplier { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var supplier = await _context.Suppliers
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
            return NotFound();

        Supplier = new SupplierDeleteViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactInfo = supplier.ContactInfo,
            ProductsCount = supplier.Products.Count,
            HasProducts = supplier.Products.Any()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
        {
            TempData["Error"] = "Поставщик не найден.";
            return RedirectToPage("./Index");
        }

        if (supplier.Products != null && supplier.Products.Any())
        {
            TempData["Error"] = "Нельзя удалить поставщика, у которого есть товары. Сначала удалите или переназначьте товары.";
            return RedirectToPage("./Index");
        }

        try
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Поставщик «{supplier.Name}» удалён.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении поставщика {SupplierId}", id);
            TempData["Error"] = "Не удалось удалить поставщика.";
        }

        return RedirectToPage("./Index");
    }
}