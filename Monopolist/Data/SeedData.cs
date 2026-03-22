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
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                LastLoginAt = DateTime.UtcNow.AddDays(-1)
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
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                LastLoginAt = DateTime.UtcNow.AddDays(-2)
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
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                LastLoginAt = DateTime.UtcNow.AddDays(-3)
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
            new Category { Name = "Крепёж" }
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
            new Supplier { Name = "ИП Петров А.В.", ContactInfo = "Мытищи, ул. Заводская, 5; +7 (495) 765-43-21" }
        };

        context.Suppliers.AddRange(suppliers);
        context.SaveChanges();
    }

    private static void SeedProducts(AppDbContext context)
    {
        if (context.Products.Any()) return;

        var сыпучие = context.Categories.FirstOrDefault(c => c.Name == "Сыпучие материалы");
        var отделочные = context.Categories.FirstOrDefault(c => c.Name == "Отделочные материалы");
        var лакокрасочные = context.Categories.FirstOrDefault(c => c.Name == "Лакокрасочные");
        var крепеж = context.Categories.FirstOrDefault(c => c.Name == "Крепёж");

        var стройРесурс = context.Suppliers.FirstOrDefault(s => s.Name == "ООО 'СтройРесурс'");
        var петров = context.Suppliers.FirstOrDefault(s => s.Name == "ИП Петров А.В.");

        var products = new List<Product>();

        if (сыпучие != null)
            products.Add(new Product
            {
                Name = "Цемент M500 50кг",
                Article = "CEM500",
                CategoryId = сыпучие.Id,
                Unit = "шт",
                PurchasePrice = 250,
                SalePrice = 350,
                CurrentStock = 24,
                SupplierId = стройРесурс?.Id,
                MinimumStock = 20,
                ImageUrl = "https://i.pinimg.com/736x/59/04/4c/59044c7163c1b54b7380ed859f1603af.jpg"
            });

        if (отделочные != null)
        {
            products.Add(new Product
            {
                Name = "Гипсокартон 12.5мм",
                Article = "GKL12.5",
                CategoryId = отделочные.Id,
                Unit = "шт",
                PurchasePrice = 400,
                SalePrice = 550,
                CurrentStock = 18,
                SupplierId = стройРесурс?.Id,
                MinimumStock = 15,
                ImageUrl = "https://i.pinimg.com/736x/65/f7/6a/65f76a591007f848ea0d084704779d16.jpg"
            });
            products.Add(new Product
            {
                Name = "Плитка керамическая",
                Article = "TILE3030",
                CategoryId = отделочные.Id,
                Unit = "шт",
                PurchasePrice = 300,
                SalePrice = 500,
                CurrentStock = 32,
                SupplierId = петров?.Id,
                MinimumStock = 20,
                ImageUrl = "https://i.pinimg.com/1200x/6d/87/12/6d87127c0b4a240723f4649a33824597.jpg"
            });
        }

        if (лакокрасочные != null)
            products.Add(new Product
            {
                Name = "Краска белая 10л",
                Article = "PAINT10",
                CategoryId = лакокрасочные.Id,
                Unit = "шт",
                PurchasePrice = 800,
                SalePrice = 1200,
                CurrentStock = 5,
                SupplierId = петров?.Id,
                MinimumStock = 10,
                ImageUrl = "https://i.pinimg.com/1200x/5b/d0/31/5bd031b93eb5621f72db076d823a106f.jpg"
            });

        if (крепеж != null)
            products.Add(new Product
            {
                Name = "Саморезы 4.2х75",
                Article = "SCR4275",
                CategoryId = крепеж.Id,
                Unit = "уп",
                PurchasePrice = 100,
                SalePrice = 180,
                CurrentStock = 8,
                SupplierId = стройРесурс?.Id,
                MinimumStock = 10,
                ImageUrl = "https://encrypted-tbn2.gstatic.com/images?q=tbn:ANd9GcQt1BpH1iHbitgcqLJy1cMfbBWKNFdI90PAGq7BrSGIp-et5tVD"
            });

        if (products.Any())
        {
            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }

    private static void SeedCustomers(AppDbContext context)
    {
        if (context.Customers.Any()) return;

        var customers = new[]
        {
            new Customer { FullName = "ООО \"СтройРисуем\"", Phone = "+7 (495) 111-22-33", Email = "info@stroyrisuem.ru", Password = "pass123", Discount = 5, RegistrationDate = DateTime.Now.AddMonths(-2) },
            new Customer { FullName = "ИП Петров А.В.", Phone = "+7 (903) 123-45-67", Email = "petrov@mail.ru", Password = "pass123", Discount = 0, RegistrationDate = DateTime.Now.AddMonths(-3) },
            new Customer { FullName = "ООО \"СтройМастер\"", Phone = "+7 (495) 222-33-44", Email = "info@stroymaster.ru", Password = "pass123", Discount = 3, RegistrationDate = DateTime.Now.AddMonths(-1) },
            new Customer { FullName = "ЖК \"Новый Город\"", Phone = "+7 (495) 333-44-55", Email = "zakupki@novgorod.ru", Password = "pass123", Discount = 7, RegistrationDate = DateTime.Now.AddDays(-20) },
            new Customer { FullName = "ИП Сидорова С.К.", Phone = "+7 (916) 555-66-77", Email = "sidorova@yandex.ru", Password = "pass123", Discount = 2, RegistrationDate = DateTime.Now.AddDays(-10) }
        };

        context.Customers.AddRange(customers);
        context.SaveChanges();
    }

    private static void SeedOrders(AppDbContext context)
    {
        if (context.Orders.Any()) return;

        var baseDate = new DateTime(2025, 2, 15);

        var customer1 = context.Customers.FirstOrDefault(c => c.FullName.Contains("СтройРисуем"));
        var customer2 = context.Customers.FirstOrDefault(c => c.FullName.Contains("Петров"));
        var customer3 = context.Customers.FirstOrDefault(c => c.FullName.Contains("СтройМастер"));
        var customer4 = context.Customers.FirstOrDefault(c => c.FullName.Contains("Новый Город"));
        var customer5 = context.Customers.FirstOrDefault(c => c.FullName.Contains("Сидорова"));

        var orders = new List<Order>();

        if (customer1 != null)
            orders.Add(new Order { OrderNumber = "ORD-2847", CustomerId = customer1.Id, OrderDate = baseDate.AddDays(-2), TotalAmount = 42800, Status = "Completed", PaymentMethod = "Card" });
        if (customer2 != null)
            orders.Add(new Order { OrderNumber = "ORD-2846", CustomerId = customer2.Id, OrderDate = baseDate.AddDays(-3), TotalAmount = 18450, Status = "Completed", PaymentMethod = "Cash" });
        if (customer3 != null)
            orders.Add(new Order { OrderNumber = "ORD-2845", CustomerId = customer3.Id, OrderDate = baseDate.AddDays(-5), TotalAmount = 67200, Status = "Completed", PaymentMethod = "Credit" });
        if (customer4 != null)
            orders.Add(new Order { OrderNumber = "ORD-2844", CustomerId = customer4.Id, OrderDate = baseDate.AddDays(-7), TotalAmount = 124300, Status = "Completed", PaymentMethod = "Card" });
        if (customer5 != null)
            orders.Add(new Order { OrderNumber = "ORD-2843", CustomerId = customer5.Id, OrderDate = baseDate.AddDays(-10), TotalAmount = 23700, Status = "Completed", PaymentMethod = "Cash" });

        if (orders.Any())
        {
            context.Orders.AddRange(orders);
            context.SaveChanges();

            var product1 = context.Products.FirstOrDefault(p => p.Name.Contains("Цемент"));
            var product2 = context.Products.FirstOrDefault(p => p.Name.Contains("Гипсокартон"));
            var product3 = context.Products.FirstOrDefault(p => p.Name.Contains("Краска"));
            var product4 = context.Products.FirstOrDefault(p => p.Name.Contains("Плитка"));
            var product5 = context.Products.FirstOrDefault(p => p.Name.Contains("Саморезы"));

            var orderItems = new List<OrderItem>();

            var order1 = context.Orders.FirstOrDefault(o => o.OrderNumber == "ORD-2847");
            var order2 = context.Orders.FirstOrDefault(o => o.OrderNumber == "ORD-2846");
            var order3 = context.Orders.FirstOrDefault(o => o.OrderNumber == "ORD-2845");
            var order4 = context.Orders.FirstOrDefault(o => o.OrderNumber == "ORD-2844");
            var order5 = context.Orders.FirstOrDefault(o => o.OrderNumber == "ORD-2843");

            if (order1 != null && product1 != null)
                orderItems.Add(new OrderItem { OrderId = order1.Id, ProductId = product1.Id, Quantity = 10, PriceAtSale = 350 });
            if (order2 != null && product2 != null)
                orderItems.Add(new OrderItem { OrderId = order2.Id, ProductId = product2.Id, Quantity = 5, PriceAtSale = 550 });
            if (order3 != null && product3 != null)
                orderItems.Add(new OrderItem { OrderId = order3.Id, ProductId = product3.Id, Quantity = 8, PriceAtSale = 1200 });
            if (order4 != null && product4 != null)
                orderItems.Add(new OrderItem { OrderId = order4.Id, ProductId = product4.Id, Quantity = 20, PriceAtSale = 500 });
            if (order5 != null && product5 != null)
                orderItems.Add(new OrderItem { OrderId = order5.Id, ProductId = product5.Id, Quantity = 30, PriceAtSale = 180 });

            if (orderItems.Any())
            {
                context.OrderItems.AddRange(orderItems);
                context.SaveChanges();
            }
        }
    }
}