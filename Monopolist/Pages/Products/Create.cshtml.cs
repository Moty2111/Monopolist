using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Models;
using Monoplist.ViewModels;
using Monoplist.Data;
using System.Security.Claims;

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

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();
        await PopulateDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserSettings();
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

            TempData["Success"] = GetLocalizedMessage($"Товар «{product.Name}» успешно добавлен.", $"Product «{product.Name}» added successfully.", $"«{product.Name}» тауары сәтті қосылды.");
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании товара");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage("Произошла ошибка при сохранении. Попробуйте снова.", "An error occurred while saving. Please try again.", "Сақтау кезінде қате орын алды. Қайталап көріңіз."));
            await LoadUserSettings();
            await PopulateDropdownsAsync(Product.CategoryId, Product.SupplierId);
            return Page();
        }
    }

    private async Task PopulateDropdownsAsync(object? selectedCategory = null, object? selectedSupplier = null)
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
        Categories = new SelectList(categories, "Value", "Text", selectedCategory);

        var suppliers = await _context.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            })
            .ToListAsync();
        suppliers.Insert(0, new SelectListItem { Value = "", Text = GetLocalizedMessage("— Не выбран —", "— Not selected —", "— Таңдалмаған —") });
        Suppliers = new SelectList(suppliers, "Value", "Text", selectedSupplier);
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