using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Warehouse;

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

    public Models.Warehouse Warehouse { get; set; } = default!;

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadUserSettings();

        var warehouse = await _context.Warehouses
            .Include(w => w.Products)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse == null)
        {
            return NotFound();
        }

        Warehouse = warehouse;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await LoadUserSettings();

        var warehouse = await _context.Warehouses
            .Include(w => w.Products)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse == null)
        {
            TempData["Error"] = GetLocalizedMessage(
                "Склад не найден.",
                "Warehouse not found.",
                "Қойма табылмады.");
            return RedirectToPage("./Index");
        }

        // Проверка наличия товаров на складе
        if (warehouse.Products != null && warehouse.Products.Any())
        {
            TempData["Error"] = GetLocalizedMessage(
                "Нельзя удалить склад, на котором есть товары. Сначала переместите или удалите товары.",
                "Cannot delete a warehouse that has products. First move or delete the products.",
                "Тауарлары бар қойманы жою мүмкін емес. Алдымен тауарларды жылжытыңыз немесе жойыңыз.");
            return RedirectToPage("./Index");
        }

        try
        {
            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                $"Склад «{warehouse.Name}» удалён.",
                $"Warehouse «{warehouse.Name}» deleted.",
                $"«{warehouse.Name}» қоймасы жойылды.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении склада {WarehouseId}", id);
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось удалить склад.",
                "Failed to delete warehouse.",
                "Қойманы жою мүмкін болмады.");
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