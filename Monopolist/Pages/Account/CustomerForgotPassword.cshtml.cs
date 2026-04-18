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

    public class InputModel
    {
        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == Input.Email);
        if (customer == null)
        {
            ModelState.AddModelError(string.Empty, "Пользователь с таким email не найден.");
            return Page();
        }

        // Генерация токена
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

        // Ссылка для сброса
        var resetLink = Url.Page("/Account/CustomerResetPassword", null, new { token, email = customer.Email }, Request.Scheme);

        // Для демонстрации выводим ссылку прямо на странице
        TempData["ResetLink"] = resetLink;
        _logger.LogInformation("Ссылка для сброса пароля для {Email}: {ResetLink}", customer.Email, resetLink);

        // Если бы был реальный email-сервис, отправляли бы письмо
        // await _emailSender.SendEmailAsync(customer.Email, "Сброс пароля", $"Ссылка: {resetLink}");

        // Очищаем модель состояния, чтобы не показывать старые ошибки
        ModelState.Clear();
        Input.Email = string.Empty; // опционально очищаем поле
        return Page();
    }
}