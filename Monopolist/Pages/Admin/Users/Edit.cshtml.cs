using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using Monopolist.ViewModels.User;

namespace Monoplist.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
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
    public UserEditViewModel UserInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        UserInput.Id = user.Id;
        UserInput.Username = user.Username;
        UserInput.Role = user.Role;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Проверка уникальности имени (если изменилось)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == UserInput.Username && u.Id != UserInput.Id);
        if (existingUser != null)
        {
            ModelState.AddModelError("UserInput.Username", "Пользователь с таким именем уже существует.");
            return Page();
        }

        try
        {
            var user = await _context.Users.FindAsync(UserInput.Id);
            if (user == null)
                return NotFound();

            user.Username = UserInput.Username;
            user.Role = UserInput.Role;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Данные пользователя «{user.Username}» обновлены.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == UserInput.Id))
                return NotFound();
            else
                throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", UserInput.Id);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении.");
            return Page();
        }
    }
}