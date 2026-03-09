using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.ComponentModel.DataAnnotations;

namespace Monoplist.Pages.Settings.Users;

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
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "Имя пользователя")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Роль обязательна")]
        [Display(Name = "Роль")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Новый пароль (оставьте пустым, если не меняете)")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        Input.Id = user.Id;
        Input.Username = user.Username;
        Input.Role = user.Role;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _context.Users.FindAsync(Input.Id);
        if (user == null)
        {
            return NotFound();
        }

        // Проверка уникальности имени, если оно изменилось
        if (user.Username != Input.Username)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Input.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Username", "Пользователь с таким именем уже существует.");
                return Page();
            }
        }

        // Обновление основных данных
        user.Username = Input.Username;
        user.Role = Input.Role;

        // Если указан новый пароль, обновляем его (без хеширования, для демо-режима)
        if (!string.IsNullOrEmpty(Input.NewPassword))
        {
            user.Password = Input.NewPassword;
        }

        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Пользователь «{user.Username}» обновлён.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == Input.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", Input.Id);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении.");
            return Page();
        }

        return RedirectToPage("./Index");
    }
}