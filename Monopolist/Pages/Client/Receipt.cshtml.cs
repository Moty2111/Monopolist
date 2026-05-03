using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Client;

[Authorize(AuthenticationSchemes = "CustomerCookie, GuestCookie")]
public class ReceiptModel : PageModel
{
    private readonly AppDbContext _context;

    public ReceiptModel(AppDbContext context)
    {
        _context = context;
    }

    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentMethodText { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<ReceiptItem> Items { get; set; } = new();

    public class ReceiptItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } = "шт";
        public decimal Total => Quantity * Price;
    }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (customerIdClaim == null || !int.TryParse(customerIdClaim, out int customerId))
            return Challenge();

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

        if (order == null)
            return NotFound("Заказ не найден или не принадлежит вам.");

        OrderNumber = order.OrderNumber;
        OrderDate = order.OrderDate;
        CustomerName = order.Customer?.FullName ?? "Не указан";
        TotalAmount = order.TotalAmount;

        PaymentMethodText = order.PaymentMethod switch
        {
            "Card" => "Банковская карта",
            "Cash" => "Наличные",
            "Credit" => "Кредит/Рассрочка",
            _ => order.PaymentMethod ?? "Не указан"
        };

        Items = order.OrderItems.Select(oi => new ReceiptItem
        {
            ProductName = oi.Product?.Name ?? "Товар удалён",
            Quantity = oi.Quantity,
            Price = oi.PriceAtSale,
            Unit = oi.Product?.Unit ?? "шт"
        }).ToList();

        return Page();
    }
}