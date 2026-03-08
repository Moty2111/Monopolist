using Microsoft.EntityFrameworkCore;
using Monoplist.Models;

namespace Monoplist.Data;

public static class SeedDataWarehouse
{
    public static void Initialize(AppDbContext context)
    {
        // Проверяем, есть ли уже склады
        if (context.Warehouses.Any()) return;

        using var transaction = context.Database.BeginTransaction();
        try
        {
            // Создаем склады с реальными изображениями
            var warehouses = new[]
            {
                new Warehouse
                {
                    Name = "Центральный склад",
                    Location = "Москва, ул. Промышленная, 15",
                    ImageUrl = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    Description = "Главный склад стройматериалов, крупногабаритные товары",
                    Capacity = 5000,
                    CurrentOccupancy = 0,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Warehouse
                {
                    Name = "Западный терминал",
                    Location = "Красногорск, ул. Складская, 8",
                    ImageUrl = "https://images.unsplash.com/photo-1566576912321-d58ddd7a6088?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    Description = "Склад отделочных материалов и инструментов",
                    Capacity = 3000,
                    CurrentOccupancy = 0,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Warehouse
                {
                    Name = "Восточный склад",
                    Location = "Люберцы, ул. Заводская, 3",
                    ImageUrl = "https://images.unsplash.com/photo-1534430480872-3498386e7856?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                    Description = "Склад лакокрасочных материалов и химии",
                    Capacity = 2000,
                    CurrentOccupancy = 0,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                }
            };

            context.Warehouses.AddRange(warehouses);
            context.SaveChanges();

            // Привязываем существующие товары к складам
            var products = context.Products.ToList();
            var central = context.Warehouses.First(w => w.Name == "Центральный склад");
            var west = context.Warehouses.First(w => w.Name == "Западный терминал");
            var east = context.Warehouses.First(w => w.Name == "Восточный склад");

            // Назначаем товары по складам
            foreach (var product in products.Where(p => p.WarehouseId == null))
            {
                if (product.Name.Contains("Цемент") || product.Name.Contains("Гипсокартон"))
                {
                    product.WarehouseId = central.Id;
                }
                else if (product.Name.Contains("Плитка") || product.Name.Contains("Саморезы"))
                {
                    product.WarehouseId = west.Id;
                }
                else if (product.Name.Contains("Краска"))
                {
                    product.WarehouseId = east.Id;
                }
            }

            // Обновляем загруженность складов с учетом товаров
            central.CurrentOccupancy = central.Products?.Sum(p => p.CurrentStock) ?? 0;
            west.CurrentOccupancy = west.Products?.Sum(p => p.CurrentStock) ?? 0;
            east.CurrentOccupancy = east.Products?.Sum(p => p.CurrentStock) ?? 0;

            context.SaveChanges();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new InvalidOperationException("Ошибка при заполнении складов", ex);
        }
    }
}