using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie, GuestCookie")]
public class ProfileModel : PageModel
{
    private readonly AppDbContext _context;

    public ProfileModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string CustomerName { get; set; } = "Гость";
    public decimal CustomerDiscount { get; set; }
    public bool IsGuest { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessage = "ФИО обязательно")]
        [StringLength(200, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 4)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string? ConfirmPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
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
                    CustomerDiscount = customer.Discount;
                    Input.FullName = customer.FullName;
                    Input.Email = customer.Email;
                    Input.Phone = customer.Phone;
                }
            }
            else
            {
                IsGuest = true;
                CustomerName = "Гость";
            }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (IsGuest) return RedirectToPage("/Account/Login");
        if (!ModelState.IsValid) return Page();

        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (customerIdClaim == null || !int.TryParse(customerIdClaim, out int customerId))
            return RedirectToPage("/Account/CustomerLogin");

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return RedirectToPage("/Account/CustomerLogin");

        customer.FullName = Input.FullName;
        customer.Email = Input.Email;
        customer.Phone = Input.Phone;
        if (!string.IsNullOrEmpty(Input.NewPassword))
            customer.Password = Input.NewPassword;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var identity = (ClaimsIdentity)User.Identity;
        var nameClaim = identity.FindFirst(ClaimTypes.Name);
        if (nameClaim != null) identity.RemoveClaim(nameClaim);
        identity.AddClaim(new Claim(ClaimTypes.Name, customer.FullName));

        TempData["Success"] = "Профиль успешно обновлён!";
        return RedirectToPage();
    }
}