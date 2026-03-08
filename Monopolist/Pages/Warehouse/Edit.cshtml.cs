using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
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
        WarehouseInput.SelectedProductIds = warehouse.Products.Select(p => p.Id).ToList(); // убрали ?.

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
            var productsToAdd = WarehouseInput.SelectedProductIds.Except(currentProductIds).ToList();
            var productsToRemove = currentProductIds.Except(WarehouseInput.SelectedProductIds).ToList();

            if (productsToAdd.Any())
            {
                var newProducts = await _context.Products
                    .Where(p => productsToAdd.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in newProducts)
                {
                    product.WarehouseId = warehouse.Id;
                    warehouse.Products.Add(product);
                }
            }

            if (productsToRemove.Any())
            {
                var productsToRemoveList = warehouse.Products
                    .Where(p => productsToRemove.Contains(p.Id))
                    .ToList();

                foreach (var product in productsToRemoveList)
                {
                    product.WarehouseId = null;
                    warehouse.Products.Remove(product);
                }
            }

            warehouse.CurrentOccupancy = warehouse.Products.Sum(p => p.CurrentStock);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Склад «{warehouse.Name}» успешно обновлен.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении склада {WarehouseId}", WarehouseInput.Id);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении.");
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
}