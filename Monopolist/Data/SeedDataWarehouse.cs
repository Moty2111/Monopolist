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
                    Location = "Челябинск, ул. Промышленная, 15",
                    ImageUrl = "https://img.freepik.com/free-photo/small-depot-employee-moves-cardboard-boxes-with-music-headset-wearing-coveralls-preparing_482257-134746.jpg?semt=ais_hybrid&w=740&q=80",
                    Description = "Главный склад стройматериалов, крупногабаритные товары",
                    Capacity = 5000,
                    CurrentOccupancy = 3240,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Warehouse
                {
                    Name = "Западный терминал",
                    Location = "Красноярск, ул. Складская, 8",
                    ImageUrl = "https://media.istockphoto.com/id/1138429558/ru/%D1%84%D0%BE%D1%82%D0%BE/%D1%80%D1%8F%D0%B4%D1%8B-%D0%BF%D0%BE%D0%BB%D0%BE%D0%BA.jpg?s=612x612&w=0&k=20&c=zkhxc5hIJLIQiWaJX_RxaiKlKuanYzSjlqFuUt6fKtA=",
                    Description = "Склад отделочных материалов и инструментов",
                    Capacity = 3000,
                    CurrentOccupancy = 1850,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Warehouse
                {
                    Name = "Восточный склад",
                    Location = "Чебаркуль, ул. Заводская, 3",
                    ImageUrl = "https://img.freepik.com/free-photo/distribution-warehouse-building-interior-large-storage-area-with-goods-shelf_342744-1452.jpg?semt=ais_hybrid&w=740&q=80",
                    Description = "Склад лакокрасочных материалов и химии",
                    Capacity = 2000,
                    CurrentOccupancy = 1200,
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
            foreach (var product in products)
            {
                if (product.Name.Contains("Цемент") || product.Name.Contains("Гипсокартон"))
                {
                    product.WarehouseId = central.Id;
                    central.CurrentOccupancy += product.CurrentStock;
                }
                else if (product.Name.Contains("Плитка") || product.Name.Contains("Саморезы"))
                {
                    product.WarehouseId = west.Id;
                    west.CurrentOccupancy += product.CurrentStock;
                }
                else if (product.Name.Contains("Краска"))
                {
                    product.WarehouseId = east.Id;
                    east.CurrentOccupancy += product.CurrentStock;
                }
            }

            // Обновляем загруженность складов с учетом товаров
            central.CurrentOccupancy = central.Products.Sum(p => p.CurrentStock);
            west.CurrentOccupancy = west.Products.Sum(p => p.CurrentStock);
            east.CurrentOccupancy = east.Products.Sum(p => p.CurrentStock);

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