using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Monoplist.Models;
using Monoplist.ViewModels;
using Monoplist.Data;
using Monopolist.ViewModels.Supplier;

namespace Monoplist.Pages.Suppliers;

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
    public SupplierCreateViewModel Supplier { get; set; } = new();

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
            var supplier = new Supplier
            {
                Name = Supplier.Name,
                ContactInfo = Supplier.ContactInfo ?? string.Empty
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Поставщик «{supplier.Name}» успешно добавлен.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании поставщика");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении. Попробуйте снова.");
            return Page();
        }
    }
}