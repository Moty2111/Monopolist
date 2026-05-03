using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Customers;

[Authorize(Roles = "Admin,Manager,Seller")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Customer Customer { get; set; } = new();

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        await LoadUserSettings();

        Customer = await _context.Customers
            .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (Customer == null) return NotFound();

        // Áûסענûי ןונוסק¸ע כמÿכüםמסעט
        await UpdateLoyaltyDiscountFast(Customer);

        return Page();
    }

    private async Task UpdateLoyaltyDiscountFast(Customer customer)
    {
        int count = await _context.Orders
            .Where(o => o.CustomerId == customer.Id && o.Status == "Completed")
            .CountAsync();

        decimal sum = await _context.Orders
            .Where(o => o.CustomerId == customer.Id && o.Status == "Completed")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        customer.TotalCompletedOrders = count;
        customer.TotalSpent = sum;
        customer.LoyaltyDiscount = (count, sum) switch
        {
            ( >= 20, >= 200000) => 7m,
            ( >= 10, >= 50000) => 5m,
            ( >= 5, >= 10000) => 3m,
            _ => 0m
        };
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
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