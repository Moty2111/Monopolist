using System.ComponentModel.DataAnnotations;

namespace Monopolist.ViewModels.Order;

public class OrderEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Выберите клиента")]
    [Display(Name = "Клиент")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Укажите сумму заказа")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
    [Display(Name = "Сумма заказа")]
    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "Выберите статус")]
    [Display(Name = "Статус")]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Метод оплаты")]
    public string? PaymentMethod { get; set; }
}