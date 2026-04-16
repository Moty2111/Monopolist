using Microsoft.AspNetCore.Authentication;
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
    private readonly IWebHostEnvironment _env;

    public ProfileModel(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? AvatarUrl { get; set; }

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

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
                    AvatarUrl = customer.AvatarUrl;
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

        // Обработка аватара
        if (AvatarFile != null && AvatarFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("AvatarFile", "Разрешены только изображения (jpg, jpeg, png, gif)");
                return Page();
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"customer_{customerId}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await AvatarFile.CopyToAsync(stream);
            }
            customer.AvatarUrl = $"/uploads/avatars/{fileName}";
        }
        else if (!string.IsNullOrEmpty(AvatarUrl))
        {
            if (AvatarUrl.Length > 500)
            {
                ModelState.AddModelError("AvatarUrl", "Слишком длинный URL аватара (максимум 500 символов)");
                return Page();
            }
            customer.AvatarUrl = AvatarUrl;
        }

        // Обновляем данные
        customer.FullName = Input.FullName;
        customer.Email = Input.Email;
        customer.Phone = Input.Phone;
        if (!string.IsNullOrEmpty(Input.NewPassword))
            customer.Password = Input.NewPassword;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Обновляем cookie аватара (для быстрого отображения, но основной источник – БД)
        if (!string.IsNullOrEmpty(customer.AvatarUrl))
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            };
            var avatarWithTimestamp = customer.AvatarUrl + "?v=" + DateTime.Now.Ticks;
            Response.Cookies.Append($"customer_avatar_{customerId}", avatarWithTimestamp, cookieOptions);
        }
        else
        {
            Response.Cookies.Delete($"customer_avatar_{customerId}");
        }

        // Принудительно обновляем аутентификацию
        await RefreshCustomerAuthentication(customer);

        TempData["Success"] = "Профиль успешно обновлён!";
        return RedirectToPage();
    }

    private async Task RefreshCustomerAuthentication(Customer customer)
    {
        await HttpContext.SignOutAsync("CustomerCookie");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, customer.FullName),
            new Claim(ClaimTypes.Role, "Customer"),
            new Claim("CustomerId", customer.Id.ToString())
        };

        if (!string.IsNullOrEmpty(customer.Email))
            claims.Add(new Claim(ClaimTypes.Email, customer.Email));
        if (!string.IsNullOrEmpty(customer.Phone))
            claims.Add(new Claim(ClaimTypes.MobilePhone, customer.Phone));

        var claimsIdentity = new ClaimsIdentity(claims, "CustomerCookie");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await HttpContext.SignInAsync("CustomerCookie", claimsPrincipal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        });
    }
}