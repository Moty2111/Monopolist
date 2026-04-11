using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Monoplist.Pages.Account;

public class GuestLoginModel : PageModel
{
    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        await HttpContext.SignOutAsync("EmployeeCookie");
        await HttpContext.SignOutAsync("CustomerCookie");
        await HttpContext.SignOutAsync("GuestCookie");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Гость"),   // ← исправлено
            new Claim(ClaimTypes.Role, "Guest"),
            new Claim("CustomerId", "0")
        };
        var identity = new ClaimsIdentity(claims, "GuestCookie");
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
        };
        await HttpContext.SignInAsync("GuestCookie", principal, authProperties);

        return LocalRedirect(returnUrl ?? "/Client/Index");
    }
}