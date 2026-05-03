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
public class ProductModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductModel> _logger;

    public ProductModel(AppDbContext context, ILogger<ProductModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public ProductDetailViewModel Product { get; set; } = new();
    public string CustomerName { get; set; } = "Гость";
    public string? AvatarUrl { get; set; }
    public decimal CustomerDiscount { get; set; }
    public bool IsGuest { get; private set; }

    // Отзывы
    public List<ReviewViewModel> Reviews { get; set; } = new();

    // Свойство для привязки формы отзыва
    [BindProperty]
    public ReviewInputModel ReviewInput { get; set; } = new();

    public class ReviewInputModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Введите текст отзыва")]
        [StringLength(2000, MinimumLength = 2)]
        public string Text { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; } = 5;
    }

    public async Task<IActionResult> OnGetAsync(int id)
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
                    CustomerDiscount = customer.EffectiveDiscount;
                    AvatarUrl = customer.AvatarUrl;
                }
            }
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        Product = new ProductDetailViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Article = product.Article,
            CategoryName = product.Category?.Name ?? "Без категории",
            Unit = product.Unit,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            CurrentStock = product.CurrentStock,
            Description = product.Warehouse?.Description ?? "Описание отсутствует",
            ImageUrl = product.ImageUrl ?? product.Warehouse?.ImageUrl,
            SupplierName = product.Supplier?.Name ?? "Не указан",
            MinimumStock = product.MinimumStock
        };

        // Загружаем только одобренные отзывы
        Reviews = await _context.Reviews
            .Where(r => r.ProductId == id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewViewModel
            {
                Id = r.Id,
                CustomerName = r.Customer.FullName,
                CustomerAvatar = r.Customer.AvatarUrl,
                Rating = r.Rating,
                Text = r.Text,
                CreatedAt = r.CreatedAt,
                AdminReply = r.AdminReply,
                RepliedAt = r.RepliedAt
            })
            .ToListAsync();

        return Page();
    }

    // Обработчик отправки отзыва
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            // Повторно загружаем страницу (с товаром и отзывами)
            await OnGetAsync(id);
            return Page();
        }

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != "Customer")
            return Challenge(); // только авторизованные клиенты

        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (!int.TryParse(customerIdClaim, out int customerId))
            return Challenge();

        var review = new Review
        {
            ProductId = id,
            CustomerId = customerId,
            Text = ReviewInput.Text,
            Rating = ReviewInput.Rating,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ваш отзыв отправлен на модерацию и появится после одобрения.";
        return RedirectToPage(new { id });
    }
}

public class ReviewViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? AdminReply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public string? CustomerAvatar { get; set; }
}

public class ProductDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = "шт";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int CurrentStock { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int MinimumStock { get; set; }
}