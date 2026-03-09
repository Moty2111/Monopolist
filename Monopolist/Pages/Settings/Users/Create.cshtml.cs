using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.ComponentModel.DataAnnotations;

namespace Monoplist.Pages.Settings.Users;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(AppDbContext context, ILogger<CreateModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "Имя пользователя")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Роль обязательна")]
        [Display(Name = "Роль")]
        public string Role { get; set; } = "Seller";
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Проверка уникальности имени пользователя
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == Input.Username);
        if (existingUser != null)
        {
            ModelState.AddModelError("Input.Username", "Пользователь с таким именем уже существует.");
            return Page();
        }

        try
        {
            // Сохраняем пароль в исходном виде (для учебного проекта)
            var user = new User
            {
                Username = Input.Username,
                Password = Input.Password, // Пароль хранится открыто (небезопасно, только для демо)
                Role = Input.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Пользователь «{user.Username}» успешно создан.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании пользователя");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении.");
            return Page();
        }
    }
}