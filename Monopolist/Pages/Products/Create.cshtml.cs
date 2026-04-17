using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.Services;
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
            // Обработка изображения
            if (imageFile != null && imageFile.Length > 0)
            {
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

            // === УВЕДОМЛЕНИЯ ===
            var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var currentUserName = User.Identity?.Name ?? "Пользователь";

            // 1. Администраторам о новом товаре
            await NotificationService.CreateForAdminsAsync(_context,
                GetLocalizedMessage("Новый товар", "New product", "Жаңа тауар"),
                GetLocalizedMessage(
                    $"Добавлен товар «{Product.Name}» (арт. {Product.Article}) пользователем {currentUserName}.",
                    $"Product «{Product.Name}» (sku {Product.Article}) added by {currentUserName}.",
                    $"«{Product.Name}» тауары ({Product.Article}) қолданушы {currentUserName} тарапынан қосылды."),
                NotificationType.Info,
                $"/Products/Details?id={Product.Id}");

            // 2. Самому создателю
            await NotificationService.CreateForUserAsync(_context, currentUserId,
                GetLocalizedMessage("Товар добавлен", "Product added", "Тауар қосылды"),
                GetLocalizedMessage(
                    $"Вы добавили товар «{Product.Name}» в каталог.",
                    $"You added product «{Product.Name}» to the catalog.",
                    $"Сіз «{Product.Name}» тауарын каталогқа қостыңыз."),
                NotificationType.Success,
                $"/Products/Details?id={Product.Id}");

            // 3. Если остаток меньше минимального – предупреждение
            if (Product.CurrentStock <= Product.MinimumStock)
            {
                await NotificationService.CreateForAdminsAsync(_context,
                    GetLocalizedMessage("Низкий остаток", "Low stock", "Аз қалдық"),
                    GetLocalizedMessage(
                        $"Товар «{Product.Name}»: остаток {Product.CurrentStock} {Product.Unit} (мин. {Product.MinimumStock}).",
                        $"Product «{Product.Name}»: stock {Product.CurrentStock} {Product.Unit} (min. {Product.MinimumStock}).",
                        $"«{Product.Name}» тауары: қалдық {Product.CurrentStock} {Product.Unit} (мин. {Product.MinimumStock})."),
                    NotificationType.Stock,
                    $"/Products/Details?id={Product.Id}");
            }

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