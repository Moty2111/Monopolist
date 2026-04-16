using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie, GuestCookie")]
public class ContactsModel : PageModel
{
    public string CustomerName { get; set; } = "Гость";
    public string? AvatarUrl { get; set; }
    public bool IsGuest { get; private set; }

    public void OnGet()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        IsGuest = role != "Customer";

        if (!IsGuest)
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (customerIdClaim != null && int.TryParse(customerIdClaim, out int customerId) && customerId > 0)
            {
                // Для статической страницы имя можно взять из claims, аватар – при желании из БД.
                var name = User.FindFirst(ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(name)) CustomerName = name;
            }
        }
    }
}