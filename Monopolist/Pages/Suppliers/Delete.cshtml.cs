using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using System.Security.Claims;

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

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        await LoadUserSettings();

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
            TempData["Error"] = GetLocalizedMessage(
                "Поставщик не найден.",
                "Supplier not found.",
                "Жеткізуші табылмады.");
            return RedirectToPage("./Index");
        }

        if (supplier.Products != null && supplier.Products.Any())
        {
            TempData["Error"] = GetLocalizedMessage(
                "Нельзя удалить поставщика, у которого есть товары. Сначала удалите или переназначьте товары.",
                "Cannot delete a supplier that has products. First delete or reassign the products.",
                "Тауарлары бар жеткізушіні жою мүмкін емес. Алдымен тауарларды жойыңыз немесе қайта тағайындаңыз.");
            return RedirectToPage("./Index");
        }

        try
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                $"Поставщик «{supplier.Name}» удалён.",
                $"Supplier «{supplier.Name}» deleted.",
                $"«{supplier.Name}» жеткізушісі жойылды.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении поставщика {SupplierId}", id);
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось удалить поставщика.",
                "Failed to delete supplier.",
                "Жеткізушіні жою мүмкін болмады.");
        }

        return RedirectToPage("./Index");
    }

    private async Task LoadUserSettings()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            Language = user.Language ?? "ru";
            CompactMode = user.CompactMode;
            Animations = user.Animations;
            Theme = user.Theme ?? "light";
            CustomColor = user.CustomColor ?? "#FF6B00";
        }
    }

    private string GetLocalizedMessage(string ru, string en, string kk)
    {
        return Language switch
        {
            "en" => en,
            "kk" => kk,
            _ => ru
        };
    }
}