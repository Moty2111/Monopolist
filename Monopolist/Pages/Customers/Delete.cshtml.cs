using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;

namespace Monoplist.Pages.Customers
{
    [Authorize(Roles = "Admin,Manager")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(AppDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Customer Customer { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            Customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (Customer == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                TempData["Error"] = "Клиент не найден.";
                return RedirectToPage("./Index");
            }

            // Проверка на наличие заказов
            bool hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == id);
            if (hasOrders)
            {
                TempData["Error"] = "Нельзя удалить клиента, у которого есть заказы. Сначала удалите заказы.";
                return RedirectToPage("./Index");
            }

            try
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Клиент удалён.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении клиента");
                TempData["Error"] = "Не удалось удалить клиента.";
            }

            return RedirectToPage("./Index");
        }
    }
}