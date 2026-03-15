using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

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
    public SupplierEditViewModel SupplierInput { get; set; } = new();

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

        SupplierInput.Id = supplier.Id;
        SupplierInput.Name = supplier.Name;
        SupplierInput.ContactInfo = supplier.ContactInfo;
        SupplierInput.SelectedProductIds = supplier.Products.Select(p => p.Id).ToList();

        await LoadAvailableProducts();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserSettings();
            await LoadAvailableProducts();
            return Page();
        }

        try
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == SupplierInput.Id);

            if (supplier == null)
                return NotFound();

            supplier.Name = SupplierInput.Name;
            supplier.ContactInfo = SupplierInput.ContactInfo ?? string.Empty;
            supplier.UpdatedAt = DateTime.UtcNow;

            var currentProductIds = supplier.Products.Select(p => p.Id).ToList();
            var selectedIds = SupplierInput.SelectedProductIds ?? new List<int>();

            var toAdd = selectedIds.Except(currentProductIds).ToList();
            var toRemove = currentProductIds.Except(selectedIds).ToList();

            if (toAdd.Any())
            {
                var productsToAdd = await _context.Products
                    .Where(p => toAdd.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in productsToAdd)
                {
                    if (product.SupplierId != null && product.SupplierId != supplier.Id)
                    {
                        ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                            $"Товар '{product.Name}' уже принадлежит другому поставщику.",
                            $"Product '{product.Name}' already belongs to another supplier.",
                            $"'{product.Name}' тауары басқа жеткізушіге тиесілі."));
                        await LoadUserSettings();
                        await LoadAvailableProducts();
                        return Page();
                    }
                    product.SupplierId = supplier.Id;
                    supplier.Products.Add(product);
                }
            }

            if (toRemove.Any())
            {
                var productsToRemove = supplier.Products
                    .Where(p => toRemove.Contains(p.Id))
                    .ToList();

                foreach (var product in productsToRemove)
                {
                    product.SupplierId = null;
                    supplier.Products.Remove(product);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                $"Поставщик «{supplier.Name}» обновлён.",
                $"Supplier «{supplier.Name}» updated.",
                $"«{supplier.Name}» жеткізушісі жаңартылды.");

            return RedirectToPage("./Index");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Suppliers.AnyAsync(s => s.Id == SupplierInput.Id))
                return NotFound();
            else
                throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении поставщика {SupplierId}", SupplierInput.Id);
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при обновлении. Попробуйте снова.",
                "An error occurred while updating. Please try again.",
                "Жаңарту кезінде қате орын алды. Қайталап көріңіз."));
            await LoadUserSettings();
            await LoadAvailableProducts();
            return Page();
        }
    }

    private async Task LoadAvailableProducts()
    {
        var products = await _context.Products
            .OrderBy(p => p.Name)
            .Select(p => new SupplierProductSelectItem
            {
                Id = p.Id,
                Name = p.Name,
                Article = p.Article,
                CurrentStock = p.CurrentStock,
                Unit = p.Unit
            })
            .ToListAsync();

        var selectedIds = SupplierInput.SelectedProductIds ?? new List<int>();
        foreach (var product in products)
        {
            product.IsSelected = selectedIds.Contains(product.Id);
        }

        SupplierInput.AvailableProducts = products;
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