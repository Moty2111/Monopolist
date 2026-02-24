using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;

namespace Monoplist.Pages.Customers;

[Authorize(Roles = "Admin,Manager")]
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
    public Customer Customer { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        Customer = await _context.Customers.FindAsync(id);
        if (Customer == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            Customer.UpdatedAt = DateTime.Now;
            _context.Attach(Customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Данные клиента обновлены.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Customers.AnyAsync(c => c.Id == Customer.Id))
                return NotFound();
            else
                throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении клиента");
            ModelState.AddModelError(string.Empty, "Произошла ошибка при сохранении. Попробуйте снова.");
            return Page();
        }
    }
}