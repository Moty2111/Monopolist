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

    // Поля для капчи
    [TempData]
    public string? CaptchaQuestion { get; set; }

    [TempData]
    public int? CaptchaAnswer { get; set; }

    [BindProperty]
    public string? CaptchaInput { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Логин обязателен.")]
        [Display(Name = "Логин")]
        public string Username { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        // Генерируем капчу при загрузке страницы
        GenerateCaptcha();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Проверяем капчу
        if (CaptchaAnswer == null || string.IsNullOrWhiteSpace(CaptchaInput) ||
            !int.TryParse(CaptchaInput, out int userAnswer) || userAnswer != CaptchaAnswer.Value)
        {
            ModelState.AddModelError("CaptchaInput", "Неверный ответ на контрольный вопрос.");
            GenerateCaptcha(); // перегенерируем вопрос
            return Page();
        }

        if (!ModelState.IsValid)
        {
            GenerateCaptcha();
            return Page();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == Input.Username);

        if (user == null)
        {
            _logger.LogWarning("Попытка восстановления пароля для несуществующего логина: {Username}", Input.Username);
            // Сообщение об успехе, чтобы не раскрывать существование пользователя
            SuccessMessage = "Если указанный логин существует, на экране отобразится одноразовый ключ. Свяжитесь с администратором для сброса пароля.";
            GenerateCaptcha();
            return Page();
        }

        // Генерация одноразового токена (8 символов)
        var token = GenerateRandomToken(8);
        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Сгенерирован ключ восстановления для {Username}: {Token}", user.Username, token);

        GeneratedToken = token;
        SuccessMessage = $"Ваш одноразовый ключ: {token}. Используйте его для сброса пароля у администратора. Ключ действителен 24 часа.";

        // Перегенерируем капчу для новой формы
        GenerateCaptcha();
        return Page();
    }

    private void GenerateCaptcha()
    {
        var rnd = new Random();
        int a = rnd.Next(1, 10);
        int b = rnd.Next(1, 10);
        CaptchaQuestion = $"Сколько будет {a} + {b}?";
        CaptchaAnswer = a + b;
    }

    private string GenerateRandomToken(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
    }
}