using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Monoplist.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(AppDbContext context, ILogger<ForgotPasswordModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? GeneratedToken { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Логин обязателен.")]
        [Display(Name = "Логин")]
        public string Username { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == Input.Username);

        if (user == null)
        {
            _logger.LogWarning("Попытка восстановления пароля для несуществующего логина: {Username}", Input.Username);
            // Не показываем, что пользователь не найден (безопасность)
            SuccessMessage = "Если указанный логин существует, администратор получит уведомление. Свяжитесь с администратором для получения одноразового ключа.";
            return Page();
        }

        // Генерация одноразового токена (8 символов: цифры и заглавные буквы)
        var token = GenerateRandomToken(8);
        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(24); // токен действует 24 часа
        await _context.SaveChangesAsync();

        _logger.LogInformation("Сгенерирован ключ восстановления для пользователя {Username}: {Token}", user.Username, token);

        GeneratedToken = token;
        SuccessMessage = $"Сгенерирован одноразовый ключ доступа: {token}\nПередайте его администратору. Администратор сможет сбросить ваш пароль.\nКлюч действителен 24 часа.";

        return Page();
    }

    private string GenerateRandomToken(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
    }
}