using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Monoplist.Pages.Account;

[Authorize(AuthenticationSchemes = "CustomerCookie")]
public class CustomerLogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync("CustomerCookie");
        return RedirectToPage("/Account/CustomerLogin");
    }
}