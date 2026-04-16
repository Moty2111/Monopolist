using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Products;

[Authorize(Roles = "Admin,Manager,Seller")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }   // <-- добавлено

    public IList<Product> Products { get; set; } = new List<Product>();
    public SelectList Categories { get; set; } = default!;  // <-- добавлено

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();
        await LoadCategories();

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{SearchString}%") ||
                EF.Functions.Like(p.Article, $"%{SearchString}%"));
        }

        if (CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == CategoryId.Value);
        }

        Products = await query.OrderBy(p => p.Name).ToListAsync();
    }

    private async Task LoadCategories()
    {
        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        Categories = new SelectList(categories, "Id", "Name");
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
}