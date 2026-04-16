using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Products;

[Authorize(Roles = "Admin,Manager")]
public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EditModel> _logger;

    public EditModel(AppDbContext context, IWebHostEnvironment environment, ILogger<EditModel> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    public SelectList Categories { get; set; } = default!;
    public SelectList Suppliers { get; set; } = default!;
    public SelectList Warehouses { get; set; } = default!;

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();
        await LoadUserSettings();
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        Product = product;
        await PopulateDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            await LoadUserSettings();
            await PopulateDropdownsAsync();
            return Page();
        }

        var productToUpdate = await _context.Products.FindAsync(Product.Id);
        if (productToUpdate == null) return NotFound();

        try
        {
            // Обработка нового изображения
            if (imageFile != null && imageFile.Length > 0)
            {
                // Проверка размера
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                        "Размер файла не должен превышать 5 МБ.",
                        "File size must not exceed 5 MB.",
                        "Файл өлшемі 5 МБ-тан аспауы керек."));
                    await LoadUserSettings();
                    await PopulateDropdownsAsync();
                    return Page();
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                        "Допустимые форматы: JPG, JPEG, PNG, GIF.",
                        "Allowed formats: JPG, JPEG, PNG, GIF.",
                        "Рұқсат етілген форматтар: JPG, JPEG, PNG, GIF."));
                    await LoadUserSettings();
                    await PopulateDropdownsAsync();
                    return Page();
                }

                // Удаление старого файла (если есть)
                if (!string.IsNullOrEmpty(productToUpdate.ImageUrl))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, productToUpdate.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Сохранение нового файла
                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadsFolder);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                productToUpdate.ImageUrl = $"/uploads/products/{fileName}";
            }

            // Обновление остальных полей
            productToUpdate.Name = Product.Name;
            productToUpdate.Article = Product.Article;
            productToUpdate.Description = Product.Description;
            productToUpdate.CategoryId = Product.CategoryId;
            productToUpdate.Unit = Product.Unit;
            productToUpdate.PurchasePrice = Product.PurchasePrice;
            productToUpdate.SalePrice = Product.SalePrice;
            productToUpdate.CurrentStock = Product.CurrentStock;
            productToUpdate.MinimumStock = Product.MinimumStock;
            productToUpdate.SupplierId = Product.SupplierId;
            productToUpdate.WarehouseId = Product.WarehouseId;
            productToUpdate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                "Товар успешно обновлён.",
                "Product updated successfully.",
                "Тауар сәтті жаңартылды.");
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
            _logger.LogError(ex, "Ошибка при обновлении товара");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при сохранении. Попробуйте снова.",
                "An error occurred while saving. Please try again.",
                "Сақтау кезінде қате орын алды. Қайталап көріңіз."));
            await LoadUserSettings();
            await PopulateDropdownsAsync();
            return Page();
        }
    }

    private async Task PopulateDropdownsAsync()
    {
        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        Categories = new SelectList(categories, "Id", "Name", Product.CategoryId);

        var suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        Suppliers = new SelectList(suppliers, "Id", "Name", Product.SupplierId);

        var warehouses = await _context.Warehouses.OrderBy(w => w.Name).ToListAsync();
        Warehouses = new SelectList(warehouses, "Id", "Name", Product.WarehouseId);
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