using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie")]
public class AboutModel : PageModel
{
    public string CustomerName { get; set; } = "Гость";

    public void OnGet()
    {
        var customerNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(customerNameClaim))
        {
            CustomerName = customerNameClaim;
        }
    }
}