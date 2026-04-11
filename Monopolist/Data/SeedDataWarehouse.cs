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
                    ImageUrl = "https://i.pinimg.com/736x/ed/57/0c/ed570cd81644ac62cdd5fad3fd83922c.jpg",
                    Description = "Главный склад стройматериалов, крупногабаритные товары",
                    Capacity = 5000,
                    CurrentOccupancy = 0,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Warehouse
                {
                    Name = "Западный терминал",
                    Location = "Красногорск, ул. Складская, 8",
                    ImageUrl = "https://i.pinimg.com/736x/ed/14/f2/ed14f203c3d43085f56e202be82d6713.jpg",
                    Description = "Склад отделочных материалов и инструментов",
                    Capacity = 3000,
                    CurrentOccupancy = 0,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Warehouse
                {
                    Name = "Восточный склад",
                    Location = "Люберцы, ул. Заводская, 3",
                    ImageUrl = "https://i.pinimg.com/736x/92/fa/bb/92fabb601f5b831dfc7315535e97c1af.jpg",
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