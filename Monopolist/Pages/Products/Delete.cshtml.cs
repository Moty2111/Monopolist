using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using System.Security.Claims;

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

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        await LoadUserSettings();

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
            TempData["Error"] = GetLocalizedMessage("Товар не найден.", "Product not found.", "Тауар табылмады.");
            return RedirectToPage("./Index");
        }

        bool hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrders)
        {
            TempData["Error"] = GetLocalizedMessage("Нельзя удалить товар, который есть в заказах.", "Cannot delete a product that is in orders.", "Тапсырыстарда бар тауарды жою мүмкін емес.");
            return RedirectToPage("./Index");
        }

        try
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage($"Товар «{product.Name}» удалён.", $"Product «{product.Name}» deleted.", $"«{product.Name}» тауары жойылды.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении товара {ProductId}", id);
            TempData["Error"] = GetLocalizedMessage("Не удалось удалить товар.", "Failed to delete product.", "Тауарды жою мүмкін болмады.");
        }

        return RedirectToPage("./Index");
    }

    private async Task LoadUserSettings()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            Language = user.Language ?? "ru";
            CompactMode = user.CompactMode;
            Animations = user.Animations;
            Theme = user.Theme ?? "light";
            CustomColor = user.CustomColor ?? "#FF6B00";
        }
    }

    private string GetLocalizedMessage(string ru, string en, string kk)
    {
        return Language switch
        {
            "en" => en,
            "kk" => kk,
            _ => ru
        };
    }
}