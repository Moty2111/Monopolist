using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie")]
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
    public string CustomerName { get; set; } = string.Empty;
    public decimal CustomerDiscount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Запрос товара с ID: {Id}", id);

        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (customerIdClaim != null && int.TryParse(customerIdClaim, out int customerId))
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer != null)
            {
                CustomerName = customer.FullName;
                CustomerDiscount = customer.Discount;
            }
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            _logger.LogWarning("Товар с ID {Id} не найден", id);
            return NotFound();
        }

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

        return Page();
    }
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