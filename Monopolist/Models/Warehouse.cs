using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monoplist.Models;

public class Warehouse
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Название склада обязательно")]
    [Display(Name = "Название")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Местоположение")]
    [StringLength(200)]
    public string? Location { get; set; }

    [Display(Name = "URL изображения")]
    [Url(ErrorMessage = "Введите корректный URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Описание")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Вместимость")]
    [Range(1, 10000, ErrorMessage = "Вместимость должна быть от 1 до 10000")]
    public int Capacity { get; set; }

    [Display(Name = "Текущая загруженность")]
    [Range(0, 10000, ErrorMessage = "Загруженность должна быть от 0 до 10000")]
    public int CurrentOccupancy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Связь с продуктами: один склад - много товаров
    public ICollection<Product> Products { get; set; } = new List<Product>();
}