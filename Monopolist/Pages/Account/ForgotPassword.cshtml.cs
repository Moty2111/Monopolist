using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Monoplist.Pages.Account
{
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
            }
            else
            {
                _logger.LogInformation("Запрос на восстановление пароля для пользователя: {Username}", Input.Username);
                // Здесь можно отправить email со ссылкой на сброс пароля.
                // В демо-версии просто показываем информационное сообщение.
            }

            // Безопасно: всегда показываем одинаковое сообщение
            SuccessMessage = "Если указанный логин существует, на привязанный к нему email будет отправлена инструкция по восстановлению пароля.";

            return Page();
        }
    }
}