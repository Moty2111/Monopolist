using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Monoplist.Data;
using Monoplist.Models;

namespace Monoplist.Pages.Customers;

[Authorize(Roles = "Admin,Manager")]
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
    public Customer Customer { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            Customer.RegistrationDate = DateTime.Now;
            _context.Customers.Add(Customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Клиент успешно добавлен.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании клиента");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении. Попробуйте снова.");
            return Page();
        }
    }
}