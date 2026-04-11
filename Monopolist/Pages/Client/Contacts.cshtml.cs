using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Monoplist.Pages.Client;
[Authorize(AuthenticationSchemes = "CustomerCookie, GuestCookie")]

public class ContactsModel : PageModel
{
    public string CustomerName { get; set; } = "Гость";
    public bool IsGuest { get; private set; }

    public void OnGet()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        IsGuest = role == "Guest";

        if (!IsGuest)
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(name))
                CustomerName = name;
        }
    }
}