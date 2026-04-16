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
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(AppDbContext context, IWebHostEnvironment environment, ILogger<CreateModel> logger)
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

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUserSettings();
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

        try
        {
            // Обработка загрузки изображения
            if (imageFile != null && imageFile.Length > 0)
            {
                // Проверка размера (5 МБ)
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

                // Проверка расширения
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

                // Сохранение файла
                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadsFolder);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                Product.ImageUrl = $"/uploads/products/{fileName}";
            }

            Product.CreatedAt = DateTime.UtcNow;
            _context.Products.Add(Product);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                "Товар успешно добавлен.",
                "Product added successfully.",
                "Тауар сәтті қосылды.");
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании товара");
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
        Categories = new SelectList(categories, "Id", "Name");

        var suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        Suppliers = new SelectList(suppliers, "Id", "Name");

        var warehouses = await _context.Warehouses.OrderBy(w => w.Name).ToListAsync();
        Warehouses = new SelectList(warehouses, "Id", "Name");
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