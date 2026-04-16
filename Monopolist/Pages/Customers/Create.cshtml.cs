using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Customers;

[Authorize(Roles = "Admin,Manager")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<CreateModel> _logger;
    private readonly IWebHostEnvironment _env;

    public CreateModel(AppDbContext context, ILogger<CreateModel> logger, IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }

    [BindProperty]
    public Customer Customer { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUserSettings();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserSettings();
            return Page();
        }

        try
        {
            // Обработка аватара
            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("AvatarFile", "Разрешены только изображения (jpg, jpeg, png, gif)");
                    await LoadUserSettings();
                    return Page();
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars", "customers");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"customer_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarFile.CopyToAsync(stream);
                }
                Customer.AvatarUrl = $"/uploads/avatars/customers/{fileName}";
            }
            // Если файл не загружен, но указан URL, оставляем его (валидация на длину по желанию)

            if (string.IsNullOrWhiteSpace(Customer.Password))
            {
                Customer.Password = GenerateTemporaryPassword();
            }

            Customer.RegistrationDate = DateTime.UtcNow;
            _context.Customers.Add(Customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                "Клиент успешно добавлен.",
                "Customer added successfully.",
                "Клиент сәтті қосылды.");
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании клиента");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при сохранении. Попробуйте снова.",
                "An error occurred while saving. Please try again.",
                "Сақтау кезінде қате орын алды. Қайталап көріңіз."));
            await LoadUserSettings();
            return Page();
        }
    }

    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
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