using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using Monopolist.ViewModels.User;
using Monoplist.Models;

namespace Monoplist.Pages.Admin.Users;

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
    public UserCreateViewModel UserInput { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Проверка уникальности имени
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == UserInput.Username);
        if (existingUser != null)
        {
            ModelState.AddModelError("UserInput.Username", "Пользователь с таким именем уже существует.");
            return Page();
        }

        try
        {
            var user = new User
            {
                Username = UserInput.Username,
                Password = UserInput.Password, // В реальном проекте следует хешировать!
                Role = UserInput.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Пользователь «{user.Username}» успешно создан.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании пользователя");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении. Попробуйте снова.");
            return Page();
        }
    }
}