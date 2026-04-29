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
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(AppDbContext context, ILogger<ProfileModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? AvatarUrl { get; set; }

    [BindProperty]
    public string? AvatarDataUrl { get; set; }

    public string CustomerName { get; set; } = "Гость";
    public decimal CustomerDiscount { get; set; }
    public bool IsGuest { get; private set; }

    // Данные для уровня лояльности
    public int TotalCompletedOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public string LoyaltyLevelName { get; set; } = "Новичок";
    public int LoyaltyPercent { get; set; }        // 0-100 заполнение шкалы прогресса
    public string NextLevelName { get; set; } = "Бронза";
    public int OrdersToNextLevel { get; set; }
    public decimal SumToNextLevel { get; set; }
    public bool IsMaxLevel { get; set; }

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
                    CustomerDiscount = customer.EffectiveDiscount; // эффективная скидка
                    AvatarUrl = customer.AvatarUrl;
                    Input.FullName = customer.FullName;
                    Input.Email = customer.Email;
                    Input.Phone = customer.Phone;

                    // Загружаем статистику для лояльности
                    TotalCompletedOrders = customer.TotalCompletedOrders;
                    TotalSpent = customer.TotalSpent;
                    CalculateLoyaltyLevel(customer);
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

    private void CalculateLoyaltyLevel(Customer customer)
    {
        int orders = TotalCompletedOrders;
        decimal sum = TotalSpent;

        if (orders >= 20 && sum >= 200000)
        {
            LoyaltyLevelName = "Золото";
            LoyaltyPercent = 100;
            IsMaxLevel = true;
            NextLevelName = "Максимум";
            OrdersToNextLevel = 0;
            SumToNextLevel = 0;
        }
        else if (orders >= 10 && sum >= 50000)
        {
            LoyaltyLevelName = "Серебро";
            // Прогресс до золота: цель – 20 заказов и 200 000
            int ordersProgress = Math.Min(orders, 20);
            decimal sumProgress = Math.Min(sum, 200000);
            int ordersPercent = (int)((double)ordersProgress / 20 * 50);
            int sumPercent = (int)((double)sumProgress / 200000 * 50);
            LoyaltyPercent = Math.Min(ordersPercent + sumPercent, 100);
            NextLevelName = "Золото";
            OrdersToNextLevel = Math.Max(0, 20 - orders);
            SumToNextLevel = Math.Max(0, 200000 - sum);
            IsMaxLevel = false;
        }
        else if (orders >= 5 && sum >= 10000)
        {
            LoyaltyLevelName = "Бронза";
            // Прогресс до серебра: 10 заказов и 50 000
            int ordersProgress = Math.Min(orders, 10);
            decimal sumProgress = Math.Min(sum, 50000);
            int ordersPercent = (int)((double)ordersProgress / 10 * 50);
            int sumPercent = (int)((double)sumProgress / 50000 * 50);
            LoyaltyPercent = Math.Min(ordersPercent + sumPercent, 100);
            NextLevelName = "Серебро";
            OrdersToNextLevel = Math.Max(0, 10 - orders);
            SumToNextLevel = Math.Max(0, 50000 - sum);
            IsMaxLevel = false;
        }
        else
        {
            LoyaltyLevelName = "Новичок";
            // Прогресс до бронзы: 5 заказов и 10 000
            int ordersProgress = Math.Min(orders, 5);
            decimal sumProgress = Math.Min(sum, 10000);
            int ordersPercent = (int)((double)ordersProgress / 5 * 50);
            int sumPercent = (int)((double)sumProgress / 10000 * 50);
            LoyaltyPercent = Math.Min(ordersPercent + sumPercent, 100);
            NextLevelName = "Бронза";
            OrdersToNextLevel = Math.Max(0, 5 - orders);
            SumToNextLevel = Math.Max(0, 10000 - sum);
            IsMaxLevel = false;
        }
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

        // Аватар
        if (!string.IsNullOrWhiteSpace(AvatarDataUrl))
        {
            customer.AvatarUrl = AvatarDataUrl;
        }
        else if (!string.IsNullOrWhiteSpace(AvatarUrl))
        {
            customer.AvatarUrl = AvatarUrl;
        }
        else
        {
            customer.AvatarUrl = null;
        }

        customer.FullName = Input.FullName;
        customer.Email = Input.Email;
        customer.Phone = Input.Phone;
        if (!string.IsNullOrEmpty(Input.NewPassword))
            customer.Password = Input.NewPassword;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Обновляем аватар в cookie
        if (!string.IsNullOrEmpty(customer.AvatarUrl))
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            };
            string avatarValue = customer.AvatarUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                ? customer.AvatarUrl
                : customer.AvatarUrl + "?v=" + DateTime.Now.Ticks;
            Response.Cookies.Append($"customer_avatar_{customerId}", avatarValue, cookieOptions);
        }
        else
        {
            Response.Cookies.Delete($"customer_avatar_{customerId}");
        }

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