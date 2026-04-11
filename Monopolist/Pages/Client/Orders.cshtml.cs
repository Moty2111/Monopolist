using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie, GuestCookie")]
public class OrdersModel : PageModel
{
    private readonly AppDbContext _context;

    public OrdersModel(AppDbContext context)
    {
        _context = context;
    }

    public List<OrderViewModel> Orders { get; set; } = new();
    public string CustomerName { get; set; } = "Гость";
    public decimal CustomerDiscount { get; set; }
    public bool IsGuest { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    public async Task OnGetAsync()
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
                }

                var query = _context.Orders
                    .Where(o => o.CustomerId == customerId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
                    query = query.Where(o => o.Status == StatusFilter);
                if (DateFrom.HasValue)
                    query = query.Where(o => o.OrderDate >= DateFrom.Value);
                if (DateTo.HasValue)
                {
                    var endDate = DateTo.Value.Date.AddDays(1);
                    query = query.Where(o => o.OrderDate < endDate);
                }

                TotalItems = await query.CountAsync();
                TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
                if (PageNumber < 1) PageNumber = 1;
                if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                Orders = orders.Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    Items = o.OrderItems.Select(oi => new OrderItemViewModel
                    {
                        ProductName = oi.Product?.Name ?? "Неизвестно",
                        Quantity = oi.Quantity,
                        Price = oi.PriceAtSale,
                        Unit = oi.Product?.Unit ?? "шт"
                    }).ToList()
                }).ToList();
            }
            else
            {
                IsGuest = true;
                CustomerName = "Гость";
            }
        }
        else
        {
            CustomerName = "Гость";
            CustomerDiscount = 0;
        }
    }

    public async Task<IActionResult> OnPostReorderAsync(int orderId)
    {
        if (IsGuest) return Unauthorized();
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (customerIdClaim == null || !int.TryParse(customerIdClaim, out int customerId))
            return Unauthorized();

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

        if (order == null) return NotFound();

        var items = order.OrderItems.Select(oi => new
        {
            productId = oi.ProductId,
            productName = oi.Product?.Name ?? "Товар",
            quantity = oi.Quantity,
            price = oi.PriceAtSale
        }).ToList();

        return new JsonResult(items);
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } = "шт";
        public decimal Total => Quantity * Price;
    }
}