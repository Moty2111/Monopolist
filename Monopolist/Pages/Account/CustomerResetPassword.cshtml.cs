using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.ComponentModel.DataAnnotations;

namespace Monoplist.Pages.Account;

[AllowAnonymous]
public class CustomerResetPasswordModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomerResetPasswordModel> _logger;

    public CustomerResetPasswordModel(AppDbContext context, ILogger<CustomerResetPasswordModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? Token { get; set; }

    [BindProperty]
    public string? Email { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите пароль")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтвердите пароль")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            return RedirectToPage("./CustomerLogin");

        Token = token;
        Email = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(Email))
        {
            ModelState.AddModelError(string.Empty, "Недостаточно данных для сброса пароля.");
            return Page();
        }

        var resetToken = await _context.CustomerPasswordResetTokens
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Token == Token && t.Customer.Email == Email && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
        {
            ModelState.AddModelError(string.Empty, "Ссылка для сброса пароля недействительна или истекла.");
            return Page();
        }

        // Обновляем пароль (в реальном проекте используйте хеширование!)
        resetToken.Customer.Password = Input.Password;
        resetToken.IsUsed = true;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Пароль успешно изменён. Теперь вы можете войти с новым паролем.";
        return RedirectToPage("./CustomerLogin");
    }
}