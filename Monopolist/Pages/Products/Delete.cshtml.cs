using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;

namespace Monoplist.Pages.Products;

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

    public ProductDeleteViewModel Product { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        Product = new ProductDeleteViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Article = product.Article,
            CategoryName = product.Category?.Name ?? "Без категории",
            Unit = product.Unit,
            SalePrice = product.SalePrice,
            CurrentStock = product.CurrentStock,
            SupplierName = product.Supplier?.Name ?? "Не указан"
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            TempData["Error"] = "Товар не найден.";
            return RedirectToPage("./Index");
        }

        // Проверка на наличие в заказах
        bool hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrders)
        {
            TempData["Error"] = "Нельзя удалить товар, который есть в заказах. Сначала удалите или измените заказы.";
            return RedirectToPage("./Index");
        }

        try
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Товар «{product.Name}» удалён.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении товара {ProductId}", id);
            TempData["Error"] = "Не удалось удалить товар.";
        }

        return RedirectToPage("./Index");
    }
}