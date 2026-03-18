using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;
using System.Security.Claims;

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

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUserSettings();
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
                    // Проверяем, не занят ли товар на другом складе
                    if (product.WarehouseId != null)
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
                    product.Warehouse = warehouse;
                    warehouse.Products.Add(product);
                }

                warehouse.CurrentOccupancy = products.Sum(p => p.CurrentStock);
            }

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                $"Склад «{warehouse.Name}» успешно создан.",
                $"Warehouse «{warehouse.Name}» created successfully.",
                $"«{warehouse.Name}» қоймасы сәтті құрылды.");

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании склада");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при сохранении. Попробуйте снова.",
                "An error occurred while saving. Please try again.",
                "Сақтау кезінде қате орын алды. Қайталап көріңіз."));
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