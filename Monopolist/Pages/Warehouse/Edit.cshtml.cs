using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Warehouse;

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
    public WarehouseEditViewModel WarehouseInput { get; set; } = new();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadUserSettings();

        var warehouse = await _context.Warehouses
            .Include(w => w.Products)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse == null)
        {
            return NotFound();
        }

        WarehouseInput.Id = warehouse.Id;
        WarehouseInput.Name = warehouse.Name;
        WarehouseInput.Location = warehouse.Location;
        WarehouseInput.ImageUrl = warehouse.ImageUrl;
        WarehouseInput.Description = warehouse.Description;
        WarehouseInput.Capacity = warehouse.Capacity;
        WarehouseInput.SelectedProductIds = warehouse.Products.Select(p => p.Id).ToList();

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
            var warehouse = await _context.Warehouses
                .Include(w => w.Products)
                .FirstOrDefaultAsync(w => w.Id == WarehouseInput.Id);

            if (warehouse == null)
            {
                return NotFound();
            }

            warehouse.Name = WarehouseInput.Name;
            warehouse.Location = WarehouseInput.Location;
            warehouse.ImageUrl = WarehouseInput.ImageUrl;
            warehouse.Description = WarehouseInput.Description;
            warehouse.Capacity = WarehouseInput.Capacity;
            warehouse.UpdatedAt = DateTime.UtcNow;

            var currentProductIds = warehouse.Products.Select(p => p.Id).ToList();
            var selectedIds = WarehouseInput.SelectedProductIds ?? new List<int>();

            var toAdd = selectedIds.Except(currentProductIds).ToList();
            var toRemove = currentProductIds.Except(selectedIds).ToList();

            if (toAdd.Any())
            {
                var productsToAdd = await _context.Products
                    .Where(p => toAdd.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in productsToAdd)
                {
                    // Проверяем, не занят ли товар на другом складе
                    if (product.WarehouseId != null && product.WarehouseId != warehouse.Id)
                    {
                        ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                            $"Товар '{product.Name}' уже находится на другом складе.",
                            $"Product '{product.Name}' is already in another warehouse.",
                            $"'{product.Name}' тауары басқа қоймада тұр."));
                        await LoadUserSettings();
                        await LoadAvailableProducts();
                        return Page();
                    }
                    product.WarehouseId = warehouse.Id;
                    warehouse.Products.Add(product);
                }
            }

            if (toRemove.Any())
            {
                var productsToRemove = warehouse.Products
                    .Where(p => toRemove.Contains(p.Id))
                    .ToList();

                foreach (var product in productsToRemove)
                {
                    product.WarehouseId = null;
                    warehouse.Products.Remove(product);
                }
            }

            warehouse.CurrentOccupancy = warehouse.Products.Sum(p => p.CurrentStock);

            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                $"Склад «{warehouse.Name}» успешно обновлён.",
                $"Warehouse «{warehouse.Name}» updated successfully.",
                $"«{warehouse.Name}» қоймасы сәтті жаңартылды.");

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении склада {WarehouseId}", WarehouseInput.Id);
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
            .Select(p => new ProductSelectItem
            {
                Id = p.Id,
                Name = p.Name,
                Article = p.Article,
                CurrentStock = p.CurrentStock,
                Unit = p.Unit
            })
            .ToListAsync();

        var selectedIds = WarehouseInput.SelectedProductIds ?? new List<int>();
        foreach (var product in products)
        {
            product.IsSelected = selectedIds.Contains(product.Id);
        }

        WarehouseInput.AvailableProducts = products;
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