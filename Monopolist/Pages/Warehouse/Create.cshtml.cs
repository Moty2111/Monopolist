using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Warehouse;

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
    public WarehouseEditViewModel WarehouseInput { get; set; } = new();

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
            var warehouse = new Models.Warehouse
            {
                Name = WarehouseInput.Name,
                Location = WarehouseInput.Location,
                ImageUrl = WarehouseInput.ImageUrl,
                Description = WarehouseInput.Description,
                Capacity = WarehouseInput.Capacity,
                CurrentOccupancy = 0,
                CreatedAt = DateTime.UtcNow
            };

            if (WarehouseInput.SelectedProductIds.Any())
            {
                var products = await _context.Products
                    .Where(p => WarehouseInput.SelectedProductIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in products)
                {
                    product.WarehouseId = warehouse.Id;
                    product.Warehouse = warehouse;
                    warehouse.Products.Add(product);
                }

                warehouse.CurrentOccupancy = products.Sum(p => p.CurrentStock);
            }

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Склад «{warehouse.Name}» успешно создан.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании склада");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении.");
            await LoadAvailableProducts();
            return Page();
        }
    }

    private async Task LoadAvailableProducts()
    {
        // Загружаем товары из БД без условия IsSelected
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

        // Теперь в памяти вычисляем IsSelected
        var selectedIds = WarehouseInput.SelectedProductIds ?? new List<int>();
        foreach (var product in products)
        {
            product.IsSelected = selectedIds.Contains(product.Id);
        }

        WarehouseInput.AvailableProducts = products;
    }
}