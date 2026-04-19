using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie, GuestCookie")]
public class AboutModel : PageModel
{
    private readonly AppDbContext _context;

    public AboutModel(AppDbContext context)
    {
        _context = context;
    }

    public string CustomerName { get; set; } = "Гость";
    public string? AvatarUrl { get; set; }
    public bool IsGuest { get; private set; }

    public async Task OnGetAsync()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        IsGuest = role != "Customer";

        if (!IsGuest)
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (customerIdClaim != null && int.TryParse(customerIdClaim, out int customerId) && customerId > 0)
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer != null)
                {
                    CustomerName = customer.FullName;
                    AvatarUrl = customer.AvatarUrl;
                }
            }
        }
    }
}