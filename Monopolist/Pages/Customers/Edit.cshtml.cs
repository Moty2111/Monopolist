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

    public EditModel(AppDbContext context, ILogger<EditModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public Customer Customer { get; set; } = new();

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

        // Проверка паролей, если указаны
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