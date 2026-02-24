using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Models;
using Monoplist.ViewModels;
using Monoplist.Data;

namespace Monoplist.Pages.Products;

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
    public ProductCreateViewModel Product { get; set; } = new();

    public SelectList Categories { get; set; } = default!;
    public SelectList Suppliers { get; set; } = default!;

    public async Task OnGetAsync()
    {
        await PopulateDropdownsAsync();
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
            var product = new Product
            {
                Name = Product.Name,
                Article = Product.Article,
                CategoryId = Product.CategoryId,
                Unit = Product.Unit,
                PurchasePrice = Product.PurchasePrice,
                SalePrice = Product.SalePrice,
                CurrentStock = Product.CurrentStock,
                SupplierId = Product.SupplierId,
                MinimumStock = Product.MinimumStock
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Товар «{product.Name}» успешно добавлен.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании товара");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении. Попробуйте снова.");
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