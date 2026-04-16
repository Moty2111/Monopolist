using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Customers;

[Authorize(Roles = "Admin,Manager")]
public class EditModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<EditModel> _logger;
    private readonly IWebHostEnvironment _env;

    public EditModel(AppDbContext context, ILogger<EditModel> logger, IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }

    [BindProperty]
    public Customer Customer { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    [BindProperty]
    public string? NewPassword { get; set; }

    [BindProperty]
    public string? ConfirmPassword { get; set; }

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();
        await LoadUserSettings();
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        Customer = customer;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserSettings();
            return Page();
        }

        var customerToUpdate = await _context.Customers.FindAsync(Customer.Id);
        if (customerToUpdate == null) return NotFound();

        // Проверка паролей
        if (!string.IsNullOrWhiteSpace(NewPassword))
        {
            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, GetLocalizedMessage("Пароли не совпадают.", "Passwords do not match.", "Құпиясөздер сәйкес келмейді."));
                await LoadUserSettings();
                return Page();
            }
            if (NewPassword.Length < 4)
            {
                ModelState.AddModelError(string.Empty, GetLocalizedMessage("Пароль должен содержать минимум 4 символа.", "Password must be at least 4 characters.", "Құпиясөз кемінде 4 таңбадан тұруы керек."));
                await LoadUserSettings();
                return Page();
            }
            customerToUpdate.Password = NewPassword;
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

                // Удаляем старый файл, если он локальный
                if (!string.IsNullOrEmpty(customerToUpdate.AvatarUrl) && customerToUpdate.AvatarUrl.StartsWith("/uploads/"))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, customerToUpdate.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars", "customers");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"customer_{customerToUpdate.Id}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarFile.CopyToAsync(stream);
                }
                customerToUpdate.AvatarUrl = $"/uploads/avatars/customers/{fileName}";
            }
            else if (!string.IsNullOrEmpty(Customer.AvatarUrl))
            {
                // Если указан URL (и файл не загружен)
                customerToUpdate.AvatarUrl = Customer.AvatarUrl;
            }

            customerToUpdate.FullName = Customer.FullName;
            customerToUpdate.Phone = Customer.Phone;
            customerToUpdate.Email = Customer.Email;
            customerToUpdate.Discount = Customer.Discount;
            customerToUpdate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                "Данные клиента обновлены.",
                "Customer data updated.",
                "Клиент деректері жаңартылды.");
            return RedirectToPage("./Index");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Customers.AnyAsync(c => c.Id == Customer.Id))
                return NotFound();
            else
                throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении клиента");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при сохранении. Попробуйте снова.",
                "An error occurred while saving. Please try again.",
                "Сақтау кезінде қате орын алды. Қайталап көріңіз."));
            await LoadUserSettings();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAvatarAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        if (!string.IsNullOrEmpty(customer.AvatarUrl) && customer.AvatarUrl.StartsWith("/uploads/"))
        {
            var filePath = Path.Combine(_env.WebRootPath, customer.AvatarUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        customer.AvatarUrl = null;
        await _context.SaveChangesAsync();

        TempData["Success"] = GetLocalizedMessage("Аватар удалён.", "Avatar deleted.", "Аватар жойылды.");
        return RedirectToPage(new { id });
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