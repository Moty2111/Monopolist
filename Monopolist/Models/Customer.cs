using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monoplist.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Поле ФИО обязательно для заполнения")]
        [Display(Name = "ФИО / Название")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Длина строки должна быть от 2 до 200 символов")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [Display(Name = "Телефон")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        [Display(Name = "Email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль должен содержать минимум 4 символа")]
        public string Password { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Скидка должна быть от 0 до 100")]
        [Display(Name = "Скидка (%)")]
        public int Discount { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Дата регистрации")]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        [Display(Name = "Дата обновления")]
        public DateTime? UpdatedAt { get; set; }

        // Аватар – может быть URL или data URL (base64). Тип nvarchar(max) позволяет хранить длинные строки.
        [Column(TypeName = "nvarchar(max)")]
        [Display(Name = "Аватар")]
        [StringLength(int.MaxValue)] // Отключает валидацию длины на клиенте, но база данных примет любое значение.
        public string? AvatarUrl { get; set; }

        // Навигационные свойства
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}