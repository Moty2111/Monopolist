using System.ComponentModel.DataAnnotations;

namespace Monoplist.Models
{
    public class Warehouse
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название склада обязательно")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Местоположение")]
        public string? Location { get; set; }

        [Display(Name = "URL изображения")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Вместимость")]
        public int Capacity { get; set; }

        [Display(Name = "Текущая загруженность")]
        public int CurrentOccupancy { get; set; }

         //public ICollection<Product> Products { get; set; }
    }
}