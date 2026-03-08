// Pages/Suppliers/Create.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Suppliers;

[Authorize(Roles = "Admin,Manager")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(AppDbContext context, ILogger<CreateModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public SupplierCreateViewModel SupplierInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAvailableProducts();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAvailableProducts();
            return Page();
        }

        try
        {
            var supplier = new Supplier
            {
                Name = SupplierInput.Name,
                ContactInfo = SupplierInput.ContactInfo ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            if (SupplierInput.SelectedProductIds.Any())
            {
                var products = await _context.Products
                    .Where(p => SupplierInput.SelectedProductIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in products)
                {
                    if (product.SupplierId != null)
                    {
                        ModelState.AddModelError(string.Empty, $"Товар '{product.Name}' уже принадлежит другому поставщику.");
                        await LoadAvailableProducts();
                        return Page();
                    }
                    product.SupplierId = supplier.Id;
                    supplier.Products.Add(product);
                }
            }

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Поставщик «{supplier.Name}» успешно добавлен.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании поставщика");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении. Попробуйте снова.");
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
}