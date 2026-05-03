using Microsoft.EntityFrameworkCore;
using Monoplist.Models;

namespace Monoplist.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        using var transaction = context.Database.BeginTransaction();
        try
        {
            SeedUsers(context);
            SeedCategories(context);
            SeedSuppliers(context);
            SeedProducts(context);
            SeedCustomers(context);
            SeedOrders(context);
            SeedReviews(context);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new InvalidOperationException("Ошибка при заполнении базы данных начальными данными.", ex);
        }
    }

    private static void SeedUsers(AppDbContext context)
    {
        if (context.Users.Any()) return;

        var users = new[]
        {
            new User
            {
                Username = "admin",
                Password = "admin123",
                Role = "Admin",
                FullName = "Иванов Иван Админович",
                Email = "admin@monoplist.ru",
                PhoneNumber = "+7 (999) 111-22-33",
                Position = "Главный администратор",
                AvatarUrl = "https://i.pinimg.com/736x/a7/1b/26/a71b266a0de157838aaea23fc4aaab7e.jpg",
                Language = "ru",
                Theme = "light",
                CompactMode = false,
                Animations = true,
                CustomColor = "#FF6B00",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                LastLoginAt = DateTime.UtcNow.AddDays(-1)
            },
            new User
            {
                Username = "admin2",
                Password = "admin123",
                Role = "Admin",
                FullName = "Козлов Дмитрий Сергеевич",
                Email = "admin2@monoplist.ru",
                PhoneNumber = "+7 (999) 111-44-55",
                Position = "Системный администратор",
                AvatarUrl = "https://i.pinimg.com/736x/54/4c/6b/544c6bb1549ee71fc5647b53646d68a9.jpg",
                Language = "ru",
                Theme = "dark",
                CompactMode = true,
                Animations = true,
                CustomColor = "#FF6B00",
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                LastLoginAt = DateTime.UtcNow.AddDays(-2)
            },
            new User
            {
                Username = "manager",
                Password = "manager123",
                Role = "Manager",
                FullName = "Петров Пётр Менеджерович",
                Email = "manager@monoplist.ru",
                PhoneNumber = "+7 (999) 222-33-44",
                Position = "Менеджер по продажам",
                AvatarUrl = "https://i.pinimg.com/736x/3d/57/49/3d574920481fffef4db99c16e90463c9.jpg",
                Language = "ru",
                Theme = "light",
                CompactMode = false,
                Animations = true,
                CustomColor = "#FF6B00",
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                LastLoginAt = DateTime.UtcNow.AddDays(-2)
            },
            new User
            {
                Username = "manager2",
                Password = "manager123",
                Role = "Manager",
                FullName = "Смирнова Елена Викторовна",
                Email = "manager2@monoplist.ru",
                PhoneNumber = "+7 (999) 222-55-66",
                Position = "Старший менеджер",
                AvatarUrl = "https://i.pinimg.com/736x/b1/2e/c1/b12ec1bc7b924d3e3ed197e35c075e0e.jpg",
                Language = "ru",
                Theme = "dark",
                CompactMode = false,
                Animations = true,
                CustomColor = "#FF6B00",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                LastLoginAt = DateTime.UtcNow.AddDays(-1)
            },
            new User
            {
                Username = "seller",
                Password = "seller123",
                Role = "Seller",
                FullName = "Сидорова Анна Продавцовна",
                Email = "seller@monoplist.ru",
                PhoneNumber = "+7 (999) 333-44-55",
                Position = "Продавец-консультант",
                AvatarUrl = "https://i.pinimg.com/736x/9e/50/fb/9e50fb03df1fed141c67f3e31e6f3c82.jpg",
                Language = "ru",
                Theme = "light",
                CompactMode = false,
                Animations = true,
                CustomColor = "#FF6B00",
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                LastLoginAt = DateTime.UtcNow.AddDays(-3)
            },
            new User
            {
                Username = "seller2",
                Password = "seller123",
                Role = "Seller",
                FullName = "Кузнецов Алексей Иванович",
                Email = "seller2@monoplist.ru",
                PhoneNumber = "+7 (999) 333-66-77",
                Position = "Продавец-кассир",
                AvatarUrl = "https://i.pinimg.com/1200x/7e/49/9c/7e499cb410da435e87d01d042226bf9a.jpg",
                Language = "ru",
                Theme = "dark",
                CompactMode = true,
                Animations = true,
                CustomColor = "#FF6B00",
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                LastLoginAt = DateTime.UtcNow.AddDays(-4)
            }
        };

        context.Users.AddRange(users);
        context.SaveChanges();
    }

    private static void SeedCategories(AppDbContext context)
    {
        if (context.Categories.Any()) return;

        var categories = new[]
        {
            new Category { Name = "Сыпучие материалы" },
            new Category { Name = "Отделочные материалы" },
            new Category { Name = "Лакокрасочные" },
            new Category { Name = "Крепёж" },
            new Category { Name = "Электроинструмент" },
            new Category { Name = "Сантехника" }
        };

        context.Categories.AddRange(categories);
        context.SaveChanges();
    }

    private static void SeedSuppliers(AppDbContext context)
    {
        if (context.Suppliers.Any()) return;

        var suppliers = new[]
        {
            new Supplier { Name = "ООО 'СтройРесурс'", ContactInfo = "Москва, ул. Строителей, 1; +7 (495) 123-45-67" },
            new Supplier { Name = "ИП Петров А.В.", ContactInfo = "Мытищи, ул. Заводская, 5; +7 (495) 765-43-21" },
            new Supplier { Name = "ООО 'ТехноСтрой'", ContactInfo = "Санкт-Петербург, пр. Энергетиков, 10; +7 (812) 555-33-22" }
        };

        context.Suppliers.AddRange(suppliers);
        context.SaveChanges();
    }

    private static void SeedProducts(AppDbContext context)
    {
        if (context.Products.Any()) return;

        var cats = context.Categories.ToList();
        var suppliers = context.Suppliers.ToList();

        var сыпучие = cats.FirstOrDefault(c => c.Name == "Сыпучие материалы");
        var отделочные = cats.FirstOrDefault(c => c.Name == "Отделочные материалы");
        var лакокрасочные = cats.FirstOrDefault(c => c.Name == "Лакокрасочные");
        var крепеж = cats.FirstOrDefault(c => c.Name == "Крепёж");
        var электро = cats.FirstOrDefault(c => c.Name == "Электроинструмент");
        var сантехника = cats.FirstOrDefault(c => c.Name == "Сантехника");

        var стройРесурс = suppliers.FirstOrDefault(s => s.Name == "ООО 'СтройРесурс'");
        var петров = suppliers.FirstOrDefault(s => s.Name == "ИП Петров А.В.");
        var техноСтрой = suppliers.FirstOrDefault(s => s.Name == "ООО 'ТехноСтрой'");

        var products = new List<Product>();

        // Сыпучие
        products.Add(new Product { Name = "Цемент M500 50кг", Article = "CEM500", CategoryId = сыпучие.Id, Unit = "шт", PurchasePrice = 250, SalePrice = 350, CurrentStock = 100, MinimumStock = 20, SupplierId = стройРесурс.Id, ImageUrl = "https://i.pinimg.com/736x/59/04/4c/59044c7163c1b54b7380ed859f1603af.jpg" });
        products.Add(new Product { Name = "Песок строительный 40кг", Article = "SAND40", CategoryId = сыпучие.Id, Unit = "шт", PurchasePrice = 80, SalePrice = 120, CurrentStock = 200, MinimumStock = 30, SupplierId = стройРесурс.Id, ImageUrl = "https://i.pinimg.com/736x/6e/0b/06/6e0b0612409b080d1188b57222c83b14.jpg" });
        products.Add(new Product { Name = "Щебень гранитный 20-40мм", Article = "GRANITE2040", CategoryId = сыпучие.Id, Unit = "т", PurchasePrice = 1200, SalePrice = 1800, CurrentStock = 50, MinimumStock = 10, SupplierId = техноСтрой.Id, ImageUrl = "https://i.pinimg.com/736x/d7/51/2c/d7512c4b9c3d7e13918f9c62f04947a7.jpg" });

        // Отделочные
        products.Add(new Product { Name = "Гипсокартон 12.5мм", Article = "GKL12.5", CategoryId = отделочные.Id, Unit = "шт", PurchasePrice = 400, SalePrice = 550, CurrentStock = 150, MinimumStock = 30, SupplierId = стройРесурс.Id, ImageUrl = "https://i.pinimg.com/736x/65/f7/6a/65f76a591007f848ea0d084704779d16.jpg" });
        products.Add(new Product { Name = "Плитка керамическая 30x30", Article = "TILE3030", CategoryId = отделочные.Id, Unit = "шт", PurchasePrice = 300, SalePrice = 500, CurrentStock = 300, MinimumStock = 50, SupplierId = петров.Id, ImageUrl = "https://i.pinimg.com/1200x/6d/87/12/6d87127c0b4a240723f4649a33824597.jpg" });
        products.Add(new Product { Name = "Ламинат 32 класс", Article = "LAM32", CategoryId = отделочные.Id, Unit = "м²", PurchasePrice = 450, SalePrice = 700, CurrentStock = 80, MinimumStock = 20, SupplierId = техноСтрой.Id, ImageUrl = "https://i.pinimg.com/1200x/8b/1e/5c/8b1e5c5edcf93b12163d3c007ccb59da.jpg" });

        // Лакокрасочные
        products.Add(new Product { Name = "Краска белая 10л", Article = "PAINT10", CategoryId = лакокрасочные.Id, Unit = "шт", PurchasePrice = 800, SalePrice = 1200, CurrentStock = 40, MinimumStock = 10, SupplierId = петров.Id, ImageUrl = "https://i.pinimg.com/1200x/5b/d0/31/5bd031b93eb5621f72db076d823a106f.jpg" });
        products.Add(new Product { Name = "Грунтовка глубокого проникновения 5л", Article = "GRUNT5", CategoryId = лакокрасочные.Id, Unit = "шт", PurchasePrice = 300, SalePrice = 450, CurrentStock = 60, MinimumStock = 15, SupplierId = стройРесурс.Id, ImageUrl = "https://i.pinimg.com/736x/27/f7/12/27f71295c0d63856f08383349bdb0c81.jpg" });

        // Крепёж
        products.Add(new Product { Name = "Саморезы 4.2х75", Article = "SCR4275", CategoryId = крепеж.Id, Unit = "уп", PurchasePrice = 100, SalePrice = 180, CurrentStock = 500, MinimumStock = 100, SupplierId = стройРесурс.Id, ImageUrl = "https://encrypted-tbn2.gstatic.com/images?q=tbn:ANd9GcQt1BpH1iHbitgcqLJy1cMfbBWKNFdI90PAGq7BrSGIp-et5tVD" });
        products.Add(new Product { Name = "Дюбель-гвоздь 6х40", Article = "DOWEL640", CategoryId = крепеж.Id, Unit = "уп", PurchasePrice = 120, SalePrice = 200, CurrentStock = 400, MinimumStock = 80, SupplierId = петров.Id, ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSz_Idp3eIsBmRCsxSA0goU2REQxkSfmIkWDw&s" });

        // Электроинструмент
        products.Add(new Product { Name = "Дрель ударная 750Вт", Article = "DRILL750", CategoryId = электро.Id, Unit = "шт", PurchasePrice = 2500, SalePrice = 3800, CurrentStock = 15, MinimumStock = 5, SupplierId = техноСтрой.Id, ImageUrl = "https://i.pinimg.com/1200x/4b/f7/20/4bf720c910e4100e8c9baec438eb3434.jpg" });
        products.Add(new Product { Name = "Шуруповёрт аккумуляторный 18В", Article = "SCRDRV18", CategoryId = электро.Id, Unit = "шт", PurchasePrice = 3200, SalePrice = 4500, CurrentStock = 10, MinimumStock = 3, SupplierId = техноСтрой.Id, ImageUrl = "https://i.pinimg.com/736x/aa/54/de/aa54dea3617161859a17b837ff2dee1e.jpg" });

        // Сантехника
        products.Add(new Product { Name = "Смеситель для кухни", Article = "MIXERKIT", CategoryId = сантехника.Id, Unit = "шт", PurchasePrice = 1500, SalePrice = 2200, CurrentStock = 25, MinimumStock = 5, SupplierId = петров.Id, ImageUrl = "https://i.pinimg.com/736x/a7/49/49/a74949b4706e5c0a57b2dfc0b02ba98e.jpg" });
        products.Add(new Product { Name = "Унитаз-компакт", Article = "WCCOMPACT", CategoryId = сантехника.Id, Unit = "шт", PurchasePrice = 4000, SalePrice = 6500, CurrentStock = 8, MinimumStock = 2, SupplierId = стройРесурс.Id, ImageUrl = "https://i.pinimg.com/736x/6b/1e/a1/6b1ea1981df2b93d97314d6ef467b55e.jpg" });

        context.Products.AddRange(products);
        context.SaveChanges();
    }

    private static void SeedCustomers(AppDbContext context)
    {
        if (context.Customers.Any()) return;

        var customers = new[]
        {
            new Customer { FullName = "ООО \"СтройРисуем\"", Phone = "+7 (495) 111-22-33", Email = "info@stroyrisuem.ru", Password = "pass123", Discount = 5, RegistrationDate = DateTime.Now.AddMonths(-6), AvatarUrl = "https://i.pinimg.com/736x/21/7f/4b/217f4b6bb6adba08e3efeef3699f63a0.jpg" },
            new Customer { FullName = "ИП Петров А.В.", Phone = "+7 (903) 123-45-67", Email = "petrov@mail.ru", Password = "pass123", Discount = 0, RegistrationDate = DateTime.Now.AddMonths(-5), AvatarUrl = "https://i.pinimg.com/736x/80/c8/9f/80c89f27c0ee6086bb962d8f83ceb61e.jpg" },
            new Customer { FullName = "ООО \"СтройМастер\"", Phone = "+7 (495) 222-33-44", Email = "info@stroymaster.ru", Password = "pass123", Discount = 3, RegistrationDate = DateTime.Now.AddMonths(-4), AvatarUrl = "https://i.pinimg.com/474x/c2/34/4b/c2344b45fcc5fc8561639312df62b35a.jpg" },
            new Customer { FullName = "ЖК \"Новый Город\"", Phone = "+7 (495) 333-44-55", Email = "zakupki@novgorod.ru", Password = "pass123", Discount = 7, RegistrationDate = DateTime.Now.AddMonths(-3), AvatarUrl = "https://i.pinimg.com/736x/f5/5b/a4/f55ba46d05f9feb76ce324f5c29ee4f8.jpg" },
            new Customer { FullName = "ИП Сидорова С.К.", Phone = "+7 (916) 555-66-77", Email = "sidorova@yandex.ru", Password = "pass123", Discount = 2, RegistrationDate = DateTime.Now.AddMonths(-2), AvatarUrl = "https://i.pinimg.com/736x/6f/82/55/6f82558099093d45b5bf8f6fd1603d2d.jpg" },
            new Customer { FullName = "ООО \"РемСтрой\"", Phone = "+7 (812) 444-55-66", Email = "remstroy@mail.ru", Password = "pass123", Discount = 4, RegistrationDate = DateTime.Now.AddMonths(-1), AvatarUrl = "https://i.pinimg.com/1200x/76/ca/ed/76caed9c3ac1189bc150ce3e72a87fe9.jpg" },
            new Customer { FullName = "ИП Козлов Д.С.", Phone = "+7 (911) 777-88-99", Email = "kozlov@yandex.ru", Password = "pass123", Discount = 1, RegistrationDate = DateTime.Now.AddDays(-15), AvatarUrl = "https://i.pinimg.com/736x/eb/49/47/eb4947c03bb338f9fd23c888faf8a726.jpg" }
        };

        context.Customers.AddRange(customers);
        context.SaveChanges();
    }

    private static void SeedOrders(AppDbContext context)
    {
        if (context.Orders.Any()) return;

        var baseDate = new DateTime(2026, 3, 15);
        var rnd = new Random();

        var allCustomers = context.Customers.ToList();
        var allProducts = context.Products.ToList();

        if (!allCustomers.Any() || !allProducts.Any()) return;

        var orders = new List<Order>();
        var orderItems = new List<OrderItem>();

        // Генерируем несколько заказов для каждого клиента
        for (int i = 0; i < allCustomers.Count; i++)
        {
            var customer = allCustomers[i];
            // Для каждого клиента создадим от 2 до 6 завершённых заказов
            int ordersCount = rnd.Next(2, 7);
            for (int j = 0; j < ordersCount; j++)
            {
                var orderDate = baseDate.AddDays(-rnd.Next(1, 90));
                var orderNumber = $"ORD-{2026000 + i * 10 + j}";
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    CustomerId = customer.Id,
                    OrderDate = orderDate,
                    TotalAmount = 0, // временно
                    Status = "Completed", // большинство завершены
                    PaymentMethod = (new[] { "Card", "Cash", "Credit" })[rnd.Next(3)]
                };
                orders.Add(order);
                context.Orders.Add(order);
            }
            // Один pending заказ (более свежий)
            var pendingOrder = new Order
            {
                OrderNumber = $"ORD-{2026500 + i}",
                CustomerId = customer.Id,
                OrderDate = DateTime.UtcNow.AddDays(-rnd.Next(1, 5)),
                TotalAmount = 0,
                Status = "Pending",
                PaymentMethod = "Card"
            };
            orders.Add(pendingOrder);
            context.Orders.Add(pendingOrder);
        }
        context.SaveChanges();

        // Добавляем позиции к каждому заказу и пересчитываем TotalAmount
        foreach (var order in orders)
        {
            int itemsCount = rnd.Next(1, 4); // 1-3 товара в заказе
            decimal total = 0;
            for (int k = 0; k < itemsCount; k++)
            {
                var product = allProducts[rnd.Next(allProducts.Count)];
                int qty = rnd.Next(1, 10);
                var oi = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = qty,
                    PriceAtSale = product.SalePrice
                };
                orderItems.Add(oi);
                total += qty * product.SalePrice;
            }
            order.TotalAmount = total;
        }

        context.OrderItems.AddRange(orderItems);
        context.SaveChanges();
    }

    private static void SeedReviews(AppDbContext context)
    {
        if (context.Reviews.Any()) return;

        var products = context.Products.Take(5).ToList();
        var customers = context.Customers.Take(5).ToList();

        if (!products.Any() || !customers.Any()) return;

        var reviews = new List<Review>
        {
            new Review { ProductId = products[0].Id, CustomerId = customers[0].Id, Text = "Цемент отличный, быстро схватывается. Доставка вовремя. Рекомендую!", Rating = 5, IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new Review { ProductId = products[0].Id, CustomerId = customers[1].Id, Text = "Качество хорошее, но упаковка могла быть прочнее.", Rating = 4, IsApproved = true, AdminReply = "Спасибо за отзыв, мы учтём пожелания.", RepliedAt = DateTime.UtcNow.AddDays(-5), CreatedAt = DateTime.UtcNow.AddDays(-12) },
            new Review { ProductId = products[0].Id, CustomerId = customers[2].Id, Text = "Быстрая доставка, всё аккуратно упаковано.", Rating = 5, IsApproved = false, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Review { ProductId = products[1].Id, CustomerId = customers[3].Id, Text = "Гипсокартон ровный, без повреждений. Удобно работать.", Rating = 5, IsApproved = true, AdminReply = "Рады, что товар оправдал ожидания!", RepliedAt = DateTime.UtcNow.AddDays(-3), CreatedAt = DateTime.UtcNow.AddDays(-15) },
            new Review { ProductId = products[1].Id, CustomerId = customers[4].Id, Text = "Немного сколов на углах, но в целом нормально.", Rating = 3, IsApproved = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Review { ProductId = products[2].Id, CustomerId = customers[0].Id, Text = "Краска укрывистая, белоснежная. Хватило на большую комнату.", Rating = 5, IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
            new Review { ProductId = products[2].Id, CustomerId = customers[1].Id, Text = "Запах слабый, сохнет быстро. Понравилась.", Rating = 4, IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-18) },
            new Review { ProductId = products[3].Id, CustomerId = customers[2].Id, Text = "Плитка красивая, ровная, хорошо режется.", Rating = 4, IsApproved = true, AdminReply = "Благодарим за отзыв! Приходите ещё.", RepliedAt = DateTime.UtcNow.AddDays(-7), CreatedAt = DateTime.UtcNow.AddDays(-25) },
            new Review { ProductId = products[3].Id, CustomerId = customers[3].Id, Text = "Упаковка подвела, но плитка целая.", Rating = 3, IsApproved = false, CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new Review { ProductId = products[4].Id, CustomerId = customers[4].Id, Text = "Саморезы острые, хорошо вкручиваются. Не ржавеют.", Rating = 5, IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-30) }
        };

        context.Reviews.AddRange(reviews);
        context.SaveChanges();
    }
}