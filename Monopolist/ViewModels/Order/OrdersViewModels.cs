using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class OrderCreateViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Выберите клиента")]
    [Display(Name = "Клиент")]
    public int CustomerId { get; set; }

    [Display(Name = "Сумма")]
    [DataType(DataType.Currency)]
    public decimal TotalAmount { get; set; }

    [Display(Name = "Статус")]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Способ оплаты")]
    public string? PaymentMethod { get; set; }

    // Позиции заказа
    public List<OrderItemViewModel> Items { get; set; } = new();

    // Для выпадающих списков
    public List<SelectItem> AvailableProducts { get; set; } = new();
}

public class OrderEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Выберите клиента")]
    [Display(Name = "Клиент")]
    public int CustomerId { get; set; }

    [Display(Name = "Сумма")]
    [DataType(DataType.Currency)]
    public decimal TotalAmount { get; set; }

    [Display(Name = "Статус")]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Способ оплаты")]
    public string? PaymentMethod { get; set; }

    // Позиции заказа
    public List<OrderItemViewModel> Items { get; set; } = new();

    // Для выпадающих списков
    public List<SelectItem> AvailableProducts { get; set; } = new();
}

public class OrderItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal Price { get; set; }
    public decimal Total => Quantity * Price;
    public string Unit { get; set; } = "шт";
}

public class OrderIndexViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
}

public class OrderDetailsViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = new();
}

public class OrderDeleteViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SelectItem
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
}