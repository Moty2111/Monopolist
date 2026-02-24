using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Products;

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
    public ProductEditViewModel Product { get; set; } = new();

    public SelectList Categories { get; set; } = default!;
    public SelectList Suppliers { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        Product.Id = product.Id;
        Product.Name = product.Name;
        Product.Article = product.Article;
        Product.CategoryId = product.CategoryId;
        Product.Unit = product.Unit;
        Product.PurchasePrice = product.PurchasePrice;
        Product.SalePrice = product.SalePrice;
        Product.CurrentStock = product.CurrentStock;
        Product.SupplierId = product.SupplierId;
        Product.MinimumStock = product.MinimumStock;

        await PopulateDropdownsAsync(Product.CategoryId, Product.SupplierId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(Product.CategoryId, Product.SupplierId);
            return Page();
        }

        try
        {
            var product = await _context.Products.FindAsync(Product.Id);
            if (product == null)
                return NotFound();

            product.Name = Product.Name;
            product.Article = Product.Article;
            product.CategoryId = Product.CategoryId;
            product.Unit = Product.Unit;
            product.PurchasePrice = Product.PurchasePrice;
            product.SalePrice = Product.SalePrice;
            product.CurrentStock = Product.CurrentStock;
            product.SupplierId = Product.SupplierId;
            product.MinimumStock = Product.MinimumStock;
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Товар «{product.Name}» обновлён.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == Product.Id))
                return NotFound();
            else
                throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении товара {ProductId}", Product.Id);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении.");
            await PopulateDropdownsAsync(Product.CategoryId, Product.SupplierId);
            return Page();
        }
    }

    private async Task PopulateDropdownsAsync(object? selectedCategory = null, object? selectedSupplier = null)
    {
        // Категории
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
        Categories = new SelectList(categories, "Value", "Text", selectedCategory);

        // Поставщики
        var suppliers = await _context.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            })
            .ToListAsync();
        suppliers.Insert(0, new SelectListItem { Value = "", Text = "— Не выбран —" });
        Suppliers = new SelectList(suppliers, "Value", "Text", selectedSupplier);
    }
}