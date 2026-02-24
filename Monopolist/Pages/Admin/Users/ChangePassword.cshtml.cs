using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using Monopolist.ViewModels.User;

namespace Monoplist.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class ChangePasswordModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChangePasswordModel> _logger;

    public ChangePasswordModel(AppDbContext context, ILogger<ChangePasswordModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public ChangePasswordViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        Input.UserId = user.Id;
        Input.Username = user.Username;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var user = await _context.Users.FindAsync(Input.UserId);
            if (user == null)
                return NotFound();

            user.Password = Input.NewPassword; // В реальном проекте следует хешировать!
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Пароль пользователя «{user.Username}» изменён.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при смене пароля пользователя {UserId}", Input.UserId);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении.");
            return Page();
        }
    }
}