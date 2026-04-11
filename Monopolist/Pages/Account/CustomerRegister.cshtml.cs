using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.ComponentModel.DataAnnotations;

namespace Monoplist.Pages.Account;

[AllowAnonymous]
public class CustomerRegisterModel : PageModel
{
    private readonly AppDbContext _context;

    public CustomerRegisterModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "ФИО обязательно")]
        [StringLength(200, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 4)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Client/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Проверка уникальности email
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Email == Input.Email);
        if (existing != null)
        {
            ModelState.AddModelError("Input.Email", "Клиент с таким email уже зарегистрирован");
            return Page();
        }

        var customer = new Customer
        {
            FullName = Input.FullName,
            Email = Input.Email,
            Phone = Input.Phone,
            Password = Input.Password, // открытый текст (только для теста)
            RegistrationDate = DateTime.UtcNow,
            Discount = 0
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Регистрация прошла успешно. Теперь вы можете войти как клиент.";
        return RedirectToPage("/Account/CustomerLogin");
    }
}