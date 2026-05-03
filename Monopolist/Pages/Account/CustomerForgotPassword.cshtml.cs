using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Monoplist.Pages.Account;

[AllowAnonymous]
public class CustomerForgotPasswordModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomerForgotPasswordModel> _logger;

    public CustomerForgotPasswordModel(AppDbContext context, ILogger<CustomerForgotPasswordModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? CaptchaQuestion { get; set; }

    [TempData]
    public int? CaptchaAnswer { get; set; }

    [BindProperty]
    public string? CaptchaInput { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        GenerateCaptcha();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CaptchaAnswer == null || string.IsNullOrWhiteSpace(CaptchaInput) ||
            !int.TryParse(CaptchaInput, out int userAnswer) || userAnswer != CaptchaAnswer.Value)
        {
            ModelState.AddModelError("CaptchaInput", "Неверный ответ на контрольный вопрос.");
            GenerateCaptcha();
            return Page();
        }

        if (!ModelState.IsValid)
        {
            GenerateCaptcha();
            return Page();
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == Input.Email);
        if (customer == null)
        {
            ModelState.AddModelError(string.Empty, "Пользователь с таким email не найден.");
            GenerateCaptcha();
            return Page();
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var resetToken = new CustomerPasswordResetToken
        {
            CustomerId = customer.Id,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false
        };
        _context.CustomerPasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        var resetLink = Url.Page("/Account/CustomerResetPassword", null, new { token, email = customer.Email }, Request.Scheme);
        TempData["ResetLink"] = resetLink;
        _logger.LogInformation("Ссылка для сброса пароля для {Email}: {ResetLink}", customer.Email, resetLink);

        ModelState.Clear();
        Input.Email = string.Empty;
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
}