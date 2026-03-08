using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Monoplist.Pages.Reports;

[Authorize(Roles = "Admin,Manager,Seller")]
public class ExportModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExportModel> _logger;

    public ExportModel(AppDbContext context, ILogger<ExportModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string type, string format, DateTime? start, DateTime? end)
    {
        try
        {
            if (string.IsNullOrEmpty(format))
            {
                return BadRequest("Не указан формат экспорта. Используйте параметр format (excel, pdf, csv, word).");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            QuestPDF.Settings.License = LicenseType.Community;

            switch (format.ToLower())
            {
                case "excel":
                    return await ExportToExcel(type, start, end);
                case "pdf":
                    return await ExportToPdf(type, start, end);
                case "csv":
                    return await ExportToCsv(type, start, end);
                case "word":
                    return await ExportToWord(type, start, end);
                default:
                    return BadRequest("Неподдерживаемый формат");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при экспорте отчета {Type} в формат {Format}", type, format);
            TempData["Error"] = "Не удалось экспортировать отчет.";
            return RedirectToPage("./Index");
        }
    }

    #region Excel Export

    private async Task<FileResult> ExportToExcel(string type, DateTime? start, DateTime? end)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add($"Отчет_{type}");

        worksheet.Cells["A1"].Value = $"Отчет по {GetReportName(type)}";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 107, 0));

        worksheet.Cells["A2"].Value = $"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}";
        worksheet.Cells["A2"].Style.Font.Size = 10;
        worksheet.Cells["A2"].Style.Font.Italic = true;

        if (start.HasValue && end.HasValue && type == "sales")
        {
            worksheet.Cells["A3"].Value = $"Период: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
            worksheet.Cells["A3"].Style.Font.Size = 10;
        }

        int startRow = 5;

        switch (type)
        {
            case "sales":
                await FillSalesExcel(worksheet, start, end, startRow);
                break;
            case "products":
                await FillProductsExcel(worksheet, startRow);
                break;
            case "customers":
                await FillCustomersExcel(worksheet, startRow);
                break;
            case "warehouse":
                await FillWarehouseExcel(worksheet, startRow);
                break;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        var bytes = await package.GetAsByteArrayAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"report_{type}_{DateTime.Now:yyyyMMddHHmm}.xlsx");
    }

    private async Task FillSalesExcel(ExcelWorksheet worksheet, DateTime? start, DateTime? end, int startRow)
    {
        var startDate = start ?? DateTime.Now.AddMonths(-1);
        var endDate = end ?? DateTime.Now;
        var endOfDay = endDate.AddDays(1).AddSeconds(-1);

        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endOfDay)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        if (!orders.Any())
        {
            worksheet.Cells[startRow, 1].Value = "Нет данных за выбранный период";
            return;
        }

        worksheet.Cells[startRow, 1].Value = "Дата";
        worksheet.Cells[startRow, 2].Value = "Номер заказа";
        worksheet.Cells[startRow, 3].Value = "Клиент";
        worksheet.Cells[startRow, 4].Value = "Телефон";
        worksheet.Cells[startRow, 5].Value = "Сумма";
        worksheet.Cells[startRow, 6].Value = "Статус";
        worksheet.Cells[startRow, 7].Value = "Оплата";
        worksheet.Cells[startRow, 8].Value = "Товары";

        using (var range = worksheet.Cells[startRow, 1, startRow, 8])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 107, 0));
            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }

        int row = startRow + 1;
        decimal totalSum = 0;

        foreach (var order in orders)
        {
            worksheet.Cells[row, 1].Value = order.OrderDate.ToString("dd.MM.yyyy");
            worksheet.Cells[row, 2].Value = order.OrderNumber;
            worksheet.Cells[row, 3].Value = order.Customer?.FullName ?? "Неизвестно";
            worksheet.Cells[row, 4].Value = order.Customer?.Phone ?? "-";
            worksheet.Cells[row, 5].Value = order.TotalAmount;
            worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00 ₽";
            worksheet.Cells[row, 6].Value = GetStatusText(order.Status);
            worksheet.Cells[row, 7].Value = GetPaymentText(order.PaymentMethod);

            var products = string.Join(", ", order.OrderItems.Select(oi => $"{oi.Product?.Name} ({oi.Quantity} шт)"));
            worksheet.Cells[row, 8].Value = products;

            totalSum += order.TotalAmount;
            row++;
        }

        worksheet.Cells[row, 4].Value = "ИТОГО:";
        worksheet.Cells[row, 4].Style.Font.Bold = true;
        worksheet.Cells[row, 5].Value = totalSum;
        worksheet.Cells[row, 5].Style.Font.Bold = true;
        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00 ₽";
    }

    private async Task FillProductsExcel(ExcelWorksheet worksheet, int startRow)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (!products.Any())
        {
            worksheet.Cells[startRow, 1].Value = "Нет данных о товарах";
            return;
        }

        worksheet.Cells[startRow, 1].Value = "Артикул";
        worksheet.Cells[startRow, 2].Value = "Наименование";
        worksheet.Cells[startRow, 3].Value = "Категория";
        worksheet.Cells[startRow, 4].Value = "Поставщик";
        worksheet.Cells[startRow, 5].Value = "Склад";
        worksheet.Cells[startRow, 6].Value = "Остаток";
        worksheet.Cells[startRow, 7].Value = "Мин. остаток";
        worksheet.Cells[startRow, 8].Value = "Цена закупки";
        worksheet.Cells[startRow, 9].Value = "Цена продажи";
        worksheet.Cells[startRow, 10].Value = "Стоимость запасов";

        using (var range = worksheet.Cells[startRow, 1, startRow, 10])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 107, 0));
            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        int row = startRow + 1;
        decimal totalValue = 0;

        foreach (var product in products)
        {
            worksheet.Cells[row, 1].Value = product.Article;
            worksheet.Cells[row, 2].Value = product.Name;
            worksheet.Cells[row, 3].Value = product.Category?.Name ?? "-";
            worksheet.Cells[row, 4].Value = product.Supplier?.Name ?? "-";
            worksheet.Cells[row, 5].Value = product.Warehouse?.Name ?? "Не назначен";
            worksheet.Cells[row, 6].Value = product.CurrentStock;
            worksheet.Cells[row, 7].Value = product.MinimumStock;
            worksheet.Cells[row, 8].Value = product.PurchasePrice;
            worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0.00 ₽";
            worksheet.Cells[row, 9].Value = product.SalePrice;
            worksheet.Cells[row, 9].Style.Numberformat.Format = "#,##0.00 ₽";

            var stockValue = product.CurrentStock * product.PurchasePrice;
            worksheet.Cells[row, 10].Value = stockValue;
            worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0.00 ₽";

            totalValue += stockValue;
            row++;
        }

        worksheet.Cells[row, 9].Value = "Общая стоимость запасов:";
        worksheet.Cells[row, 9].Style.Font.Bold = true;
        worksheet.Cells[row, 10].Value = totalValue;
        worksheet.Cells[row, 10].Style.Font.Bold = true;
        worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0.00 ₽";
    }

    private async Task FillCustomersExcel(ExcelWorksheet worksheet, int startRow)
    {
        var customers = await _context.Customers
            .Include(c => c.Orders)
            .OrderBy(c => c.FullName)
            .ToListAsync();

        if (!customers.Any())
        {
            worksheet.Cells[startRow, 1].Value = "Нет данных о клиентах";
            return;
        }

        worksheet.Cells[startRow, 1].Value = "ФИО / Название";
        worksheet.Cells[startRow, 2].Value = "Телефон";
        worksheet.Cells[startRow, 3].Value = "Email";
        worksheet.Cells[startRow, 4].Value = "Скидка";
        worksheet.Cells[startRow, 5].Value = "Дата регистрации";
        worksheet.Cells[startRow, 6].Value = "Кол-во заказов";
        worksheet.Cells[startRow, 7].Value = "Сумма заказов";

        using (var range = worksheet.Cells[startRow, 1, startRow, 7])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 107, 0));
            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        int row = startRow + 1;
        decimal totalSpent = 0;

        foreach (var customer in customers)
        {
            var orders = customer.Orders?.ToList() ?? new();
            var orderCount = orders.Count;
            var orderSum = orders.Sum(o => o.TotalAmount);

            worksheet.Cells[row, 1].Value = customer.FullName;
            worksheet.Cells[row, 2].Value = customer.Phone ?? "-";
            worksheet.Cells[row, 3].Value = customer.Email ?? "-";
            worksheet.Cells[row, 4].Value = customer.Discount;
            worksheet.Cells[row, 5].Value = customer.RegistrationDate.ToString("dd.MM.yyyy");
            worksheet.Cells[row, 6].Value = orderCount;
            worksheet.Cells[row, 7].Value = orderSum;
            worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00 ₽";

            totalSpent += orderSum;
            row++;
        }

        worksheet.Cells[row, 6].Value = "ИТОГО:";
        worksheet.Cells[row, 6].Style.Font.Bold = true;
        worksheet.Cells[row, 7].Value = totalSpent;
        worksheet.Cells[row, 7].Style.Font.Bold = true;
        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00 ₽";
    }

    private async Task FillWarehouseExcel(ExcelWorksheet worksheet, int startRow)
    {
        var warehouses = await _context.Warehouses
            .Include(w => w.Products)
            .OrderBy(w => w.Name)
            .ToListAsync();

        if (!warehouses.Any())
        {
            worksheet.Cells[startRow, 1].Value = "Нет данных о складах";
            return;
        }

        worksheet.Cells[startRow, 1].Value = "Название склада";
        worksheet.Cells[startRow, 2].Value = "Местоположение";
        worksheet.Cells[startRow, 3].Value = "Вместимость";
        worksheet.Cells[startRow, 4].Value = "Текущая загрузка";
        worksheet.Cells[startRow, 5].Value = "Загрузка %";
        worksheet.Cells[startRow, 6].Value = "Кол-во товаров";
        worksheet.Cells[startRow, 7].Value = "Стоимость товаров";

        using (var range = worksheet.Cells[startRow, 1, startRow, 7])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 107, 0));
            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        int row = startRow + 1;
        int totalCapacity = 0;
        int totalOccupancy = 0;
        decimal totalValue = 0;

        foreach (var warehouse in warehouses)
        {
            var occupancy = warehouse.Products?.Sum(p => p.CurrentStock) ?? 0;
            var occupancyPercent = warehouse.Capacity > 0 ? (occupancy * 100.0 / warehouse.Capacity) : 0;
            var stockValue = warehouse.Products?.Sum(p => p.CurrentStock * p.PurchasePrice) ?? 0;

            worksheet.Cells[row, 1].Value = warehouse.Name;
            worksheet.Cells[row, 2].Value = warehouse.Location ?? "-";
            worksheet.Cells[row, 3].Value = warehouse.Capacity;
            worksheet.Cells[row, 4].Value = occupancy;
            worksheet.Cells[row, 5].Value = occupancyPercent;
            worksheet.Cells[row, 5].Style.Numberformat.Format = "0.0%";
            worksheet.Cells[row, 6].Value = warehouse.Products?.Count ?? 0;
            worksheet.Cells[row, 7].Value = stockValue;
            worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00 ₽";

            totalCapacity += warehouse.Capacity;
            totalOccupancy += occupancy;
            totalValue += stockValue;
            row++;
        }

        var totalPercent = totalCapacity > 0 ? (totalOccupancy * 100.0 / totalCapacity) : 0;
        worksheet.Cells[row, 2].Value = "ИТОГО:";
        worksheet.Cells[row, 2].Style.Font.Bold = true;
        worksheet.Cells[row, 3].Value = totalCapacity;
        worksheet.Cells[row, 3].Style.Font.Bold = true;
        worksheet.Cells[row, 4].Value = totalOccupancy;
        worksheet.Cells[row, 4].Style.Font.Bold = true;
        worksheet.Cells[row, 5].Value = totalPercent;
        worksheet.Cells[row, 5].Style.Numberformat.Format = "0.0%";
        worksheet.Cells[row, 5].Style.Font.Bold = true;
        worksheet.Cells[row, 7].Value = totalValue;
        worksheet.Cells[row, 7].Style.Font.Bold = true;
        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00 ₽";
    }

    #endregion

    #region PDF Export

    private async Task<FileResult> ExportToPdf(string type, DateTime? start, DateTime? end)
    {
        try
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Отчет по {GetReportName(type)}")
                                    .FontSize(16).Bold().FontColor(QuestPDF.Helpers.Colors.Red.Medium);
                                col.Item().Text($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}")
                                    .FontSize(8).Italic();
                            });
                        });

                    page.Content()
                        .Column(col =>
                        {
                            col.Spacing(10);

                            if (start.HasValue && end.HasValue && type == "sales")
                            {
                                col.Item().Text($"Период: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}")
                                    .FontSize(10);
                            }

                            col.Item().Element(container => BuildPdfContent(container, type, start, end));
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Страница ");
                            x.CurrentPageNumber();
                            x.Span(" из ");
                            x.TotalPages();
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"report_{type}_{DateTime.Now:yyyyMMddHHmm}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации PDF для отчета {Type}", type);
            throw;
        }
    }

    private void BuildPdfContent(QuestPDF.Infrastructure.IContainer container, string type, DateTime? start, DateTime? end)
    {
        switch (type)
        {
            case "sales":
                BuildSalesPdf(container, start, end);
                break;
            case "products":
                BuildProductsPdf(container);
                break;
            case "customers":
                BuildCustomersPdf(container);
                break;
            case "warehouse":
                BuildWarehousePdf(container);
                break;
        }
    }

    private async void BuildSalesPdf(QuestPDF.Infrastructure.IContainer container, DateTime? start, DateTime? end)
    {
        var startDate = start ?? DateTime.Now.AddMonths(-1);
        var endDate = end ?? DateTime.Now;
        var endOfDay = endDate.AddDays(1).AddSeconds(-1);

        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endOfDay)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        if (!orders.Any())
        {
            container.Column(col =>
            {
                col.Item().Text("Нет данных за выбранный период").FontSize(12).Italic();
            });
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellHeaderStyle).Text("Дата");
                header.Cell().Element(CellHeaderStyle).Text("Номер");
                header.Cell().Element(CellHeaderStyle).Text("Клиент");
                header.Cell().Element(CellHeaderStyle).Text("Сумма").AlignRight();

                static IContainer CellHeaderStyle(IContainer container) =>
                    container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Black);
            });

            decimal totalSum = 0;
            foreach (var order in orders)
            {
                table.Cell().Element(CellStyle).Text(order.OrderDate.ToString("dd.MM.yyyy"));
                table.Cell().Element(CellStyle).Text(order.OrderNumber);
                table.Cell().Element(CellStyle).Text(order.Customer?.FullName ?? "Неизвестно");
                table.Cell().Element(CellStyle).Text($"{order.TotalAmount:N0} ₽").AlignRight();
                totalSum += order.TotalAmount;

                static IContainer CellStyle(IContainer container) =>
                    container.PaddingVertical(3).BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
            }

            // Итоговая строка
            table.Cell().ColumnSpan(3).Element(TotalCellStyle).Text("ИТОГО:").AlignRight();
            table.Cell().Element(TotalCellStyle).Text($"{totalSum:N0} ₽").AlignRight();

            static IContainer TotalCellStyle(IContainer container) =>
                container.PaddingVertical(5).BorderTop(1).BorderColor(QuestPDF.Helpers.Colors.Black).DefaultTextStyle(x => x.Bold());
        });
    }

    private async void BuildProductsPdf(QuestPDF.Infrastructure.IContainer container)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Warehouse)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (!products.Any())
        {
            container.Column(col =>
            {
                col.Item().Text("Нет данных о товарах").FontSize(12).Italic();
            });
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellHeaderStyle).Text("Товар");
                header.Cell().Element(CellHeaderStyle).Text("Категория");
                header.Cell().Element(CellHeaderStyle).Text("Остаток");
                header.Cell().Element(CellHeaderStyle).Text("Мин.");
                header.Cell().Element(CellHeaderStyle).Text("Склад");

                static IContainer CellHeaderStyle(IContainer container) =>
                    container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Black);
            });

            foreach (var p in products.Take(30))
            {
                table.Cell().Element(CellStyle).Text(p.Name);
                table.Cell().Element(CellStyle).Text(p.Category?.Name ?? "-");
                table.Cell().Element(CellStyle).Text(p.CurrentStock.ToString());
                table.Cell().Element(CellStyle).Text(p.MinimumStock.ToString());
                table.Cell().Element(CellStyle).Text(p.Warehouse?.Name ?? "-");

                static IContainer CellStyle(IContainer container) =>
                    container.PaddingVertical(3).BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
            }

            if (products.Count > 30)
            {
                table.Cell().ColumnSpan(5).Text($"... и еще {products.Count - 30} товаров").Italic().FontSize(9);
            }
        });
    }

    private async void BuildCustomersPdf(QuestPDF.Infrastructure.IContainer container)
    {
        var customers = await _context.Customers
            .Include(c => c.Orders)
            .OrderBy(c => c.FullName)
            .ToListAsync();

        if (!customers.Any())
        {
            container.Column(col =>
            {
                col.Item().Text("Нет данных о клиентах").FontSize(12).Italic();
            });
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellHeaderStyle).Text("Клиент");
                header.Cell().Element(CellHeaderStyle).Text("Телефон");
                header.Cell().Element(CellHeaderStyle).Text("Заказов");
                header.Cell().Element(CellHeaderStyle).Text("Сумма").AlignRight();

                static IContainer CellHeaderStyle(IContainer container) =>
                    container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Black);
            });

            decimal totalSpent = 0;
            foreach (var c in customers.OrderByDescending(c => c.Orders?.Sum(o => o.TotalAmount) ?? 0).Take(20))
            {
                var orderCount = c.Orders?.Count ?? 0;
                var orderSum = c.Orders?.Sum(o => o.TotalAmount) ?? 0;

                table.Cell().Element(CellStyle).Text(c.FullName);
                table.Cell().Element(CellStyle).Text(c.Phone ?? "-");
                table.Cell().Element(CellStyle).Text(orderCount.ToString());
                table.Cell().Element(CellStyle).Text($"{orderSum:N0} ₽").AlignRight();
                totalSpent += orderSum;

                static IContainer CellStyle(IContainer container) =>
                    container.PaddingVertical(3).BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
            }
        });
    }

    private async void BuildWarehousePdf(QuestPDF.Infrastructure.IContainer container)
    {
        var warehouses = await _context.Warehouses
            .Include(w => w.Products)
            .ToListAsync();

        if (!warehouses.Any())
        {
            container.Column(col =>
            {
                col.Item().Text("Нет данных о складах").FontSize(12).Italic();
            });
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellHeaderStyle).Text("Склад");
                header.Cell().Element(CellHeaderStyle).Text("Вместимость");
                header.Cell().Element(CellHeaderStyle).Text("Загрузка");
                header.Cell().Element(CellHeaderStyle).Text("Товаров").AlignRight();

                static IContainer CellHeaderStyle(IContainer container) =>
                    container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Black);
            });

            foreach (var w in warehouses)
            {
                var occupancy = w.Products?.Sum(p => p.CurrentStock) ?? 0;
                var percent = w.Capacity > 0 ? occupancy * 100 / w.Capacity : 0;

                table.Cell().Element(CellStyle).Text(w.Name);
                table.Cell().Element(CellStyle).Text(w.Capacity.ToString());
                table.Cell().Element(CellStyle).Text($"{percent}%");
                table.Cell().Element(CellStyle).Text((w.Products?.Count ?? 0).ToString()).AlignRight();

                static IContainer CellStyle(IContainer container) =>
                    container.PaddingVertical(3).BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
            }
        });
    }

    #endregion

    #region CSV Export

    private async Task<FileResult> ExportToCsv(string type, DateTime? start, DateTime? end)
    {
        var sb = new StringBuilder();

        switch (type)
        {
            case "sales":
                await BuildSalesCsv(sb, start, end);
                break;
            case "products":
                await BuildProductsCsv(sb);
                break;
            case "customers":
                await BuildCustomersCsv(sb);
                break;
            case "warehouse":
                await BuildWarehouseCsv(sb);
                break;
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"report_{type}_{DateTime.Now:yyyyMMddHHmm}.csv");
    }

    private async Task BuildSalesCsv(StringBuilder sb, DateTime? start, DateTime? end)
    {
        var startDate = start ?? DateTime.Now.AddMonths(-1);
        var endDate = end ?? DateTime.Now;
        var endOfDay = endDate.AddDays(1).AddSeconds(-1);

        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endOfDay)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        sb.AppendLine("Дата;Номер заказа;Клиент;Телефон;Сумма;Статус;Оплата");

        foreach (var order in orders)
        {
            sb.AppendLine($"{order.OrderDate:dd.MM.yyyy};{order.OrderNumber};{order.Customer?.FullName ?? "Неизвестно"};{order.Customer?.Phone ?? "-"};{order.TotalAmount};{GetStatusText(order.Status)};{GetPaymentText(order.PaymentMethod)}");
        }

        if (!orders.Any())
        {
            sb.AppendLine("Нет данных за выбранный период");
        }
    }

    private async Task BuildProductsCsv(StringBuilder sb)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Warehouse)
            .OrderBy(p => p.Name)
            .ToListAsync();

        sb.AppendLine("Артикул;Наименование;Категория;Склад;Остаток;Мин.остаток;Цена закупки;Цена продажи");

        foreach (var p in products)
        {
            sb.AppendLine($"{p.Article};{p.Name};{p.Category?.Name ?? "-"};{p.Warehouse?.Name ?? "-"};{p.CurrentStock};{p.MinimumStock};{p.PurchasePrice};{p.SalePrice}");
        }

        if (!products.Any())
        {
            sb.AppendLine("Нет данных о товарах");
        }
    }

    private async Task BuildCustomersCsv(StringBuilder sb)
    {
        var customers = await _context.Customers
            .Include(c => c.Orders)
            .OrderBy(c => c.FullName)
            .ToListAsync();

        sb.AppendLine("ФИО;Телефон;Email;Скидка;Дата регистрации;Кол-во заказов;Сумма заказов");

        foreach (var c in customers)
        {
            var orderCount = c.Orders?.Count ?? 0;
            var orderSum = c.Orders?.Sum(o => o.TotalAmount) ?? 0;
            sb.AppendLine($"{c.FullName};{c.Phone ?? "-"};{c.Email ?? "-"};{c.Discount};{c.RegistrationDate:dd.MM.yyyy};{orderCount};{orderSum}");
        }

        if (!customers.Any())
        {
            sb.AppendLine("Нет данных о клиентах");
        }
    }

    private async Task BuildWarehouseCsv(StringBuilder sb)
    {
        var warehouses = await _context.Warehouses
            .Include(w => w.Products)
            .OrderBy(w => w.Name)
            .ToListAsync();

        sb.AppendLine("Название;Местоположение;Вместимость;Текущая загрузка;Загрузка %;Кол-во товаров;Стоимость товаров");

        foreach (var w in warehouses)
        {
            var occupancy = w.Products?.Sum(p => p.CurrentStock) ?? 0;
            var percent = w.Capacity > 0 ? occupancy * 100.0 / w.Capacity : 0;
            var stockValue = w.Products?.Sum(p => p.CurrentStock * p.PurchasePrice) ?? 0;
            sb.AppendLine($"{w.Name};{w.Location ?? "-"};{w.Capacity};{occupancy};{percent:F1}%;{w.Products?.Count ?? 0};{stockValue}");
        }

        if (!warehouses.Any())
        {
            sb.AppendLine("Нет данных о складах");
        }
    }

    #endregion

    #region Word Export (HTML)

    private async Task<FileResult> ExportToWord(string type, DateTime? start, DateTime? end)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1 { color: #FF6B00; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
        sb.AppendLine("th { background: #FF6B00; color: white; padding: 8px; text-align: left; }");
        sb.AppendLine("td { border: 1px solid #ddd; padding: 6px; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine(".total { font-weight: bold; background-color: #FFE5D0; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"<h1>Отчет по {GetReportName(type)}</h1>");
        sb.AppendLine($"<p><strong>Сформировано:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>");

        if (start.HasValue && end.HasValue && type == "sales")
        {
            sb.AppendLine($"<p><strong>Период:</strong> {start:dd.MM.yyyy} - {end:dd.MM.yyyy}</p>");
        }

        switch (type)
        {
            case "sales":
                await BuildSalesHtml(sb, start, end);
                break;
            case "products":
                await BuildProductsHtml(sb);
                break;
            case "customers":
                await BuildCustomersHtml(sb);
                break;
            case "warehouse":
                await BuildWarehouseHtml(sb);
                break;
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "application/msword", $"report_{type}_{DateTime.Now:yyyyMMddHHmm}.doc");
    }

    private async Task BuildSalesHtml(StringBuilder sb, DateTime? start, DateTime? end)
    {
        var startDate = start ?? DateTime.Now.AddMonths(-1);
        var endDate = end ?? DateTime.Now;
        var endOfDay = endDate.AddDays(1).AddSeconds(-1);

        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endOfDay)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        if (!orders.Any())
        {
            sb.AppendLine("<p>Нет данных за выбранный период</p>");
            return;
        }

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Дата</th><th>Номер заказа</th><th>Клиент</th><th>Телефон</th><th>Сумма</th><th>Статус</th><th>Оплата</th></tr></thead>");
        sb.AppendLine("<tbody>");

        decimal totalSum = 0;
        foreach (var order in orders)
        {
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{order.OrderDate:dd.MM.yyyy}</td>");
            sb.AppendLine($"<td>{order.OrderNumber}</td>");
            sb.AppendLine($"<td>{order.Customer?.FullName ?? "Неизвестно"}</td>");
            sb.AppendLine($"<td>{order.Customer?.Phone ?? "-"}</td>");
            sb.AppendLine($"<td>{order.TotalAmount:N0} ₽</td>");
            sb.AppendLine($"<td>{GetStatusText(order.Status)}</td>");
            sb.AppendLine($"<td>{GetPaymentText(order.PaymentMethod)}</td>");
            sb.AppendLine($"</tr>");
            totalSum += order.TotalAmount;
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine($"<tfoot><tr class='total'><td colspan='4' style='text-align:right;'>ИТОГО:</td><td>{totalSum:N0} ₽</td><td colspan='2'></td></tr></tfoot>");
        sb.AppendLine("</table>");
    }

    private async Task BuildProductsHtml(StringBuilder sb)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Warehouse)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (!products.Any())
        {
            sb.AppendLine("<p>Нет данных о товарах</p>");
            return;
        }

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Артикул</th><th>Наименование</th><th>Категория</th><th>Склад</th><th>Остаток</th><th>Мин.остаток</th><th>Цена закупки</th><th>Цена продажи</th></tr></thead>");
        sb.AppendLine("<tbody>");

        decimal totalValue = 0;
        foreach (var p in products)
        {
            var stockValue = p.CurrentStock * p.PurchasePrice;
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{p.Article}</td>");
            sb.AppendLine($"<td>{p.Name}</td>");
            sb.AppendLine($"<td>{p.Category?.Name ?? "-"}</td>");
            sb.AppendLine($"<td>{p.Warehouse?.Name ?? "-"}</td>");
            sb.AppendLine($"<td>{p.CurrentStock}</td>");
            sb.AppendLine($"<td>{p.MinimumStock}</td>");
            sb.AppendLine($"<td>{p.PurchasePrice:N0} ₽</td>");
            sb.AppendLine($"<td>{p.SalePrice:N0} ₽</td>");
            sb.AppendLine($"</tr>");
            totalValue += stockValue;
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine($"<tfoot><tr class='total'><td colspan='5' style='text-align:right;'>Общая стоимость запасов:</td><td colspan='3'>{totalValue:N0} ₽</td></tr></tfoot>");
        sb.AppendLine("</table>");
    }

    private async Task BuildCustomersHtml(StringBuilder sb)
    {
        var customers = await _context.Customers
            .Include(c => c.Orders)
            .OrderBy(c => c.FullName)
            .ToListAsync();

        if (!customers.Any())
        {
            sb.AppendLine("<p>Нет данных о клиентах</p>");
            return;
        }

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>ФИО</th><th>Телефон</th><th>Email</th><th>Скидка</th><th>Дата регистрации</th><th>Кол-во заказов</th><th>Сумма заказов</th></tr></thead>");
        sb.AppendLine("<tbody>");

        decimal totalSpent = 0;
        foreach (var c in customers)
        {
            var orderCount = c.Orders?.Count ?? 0;
            var orderSum = c.Orders?.Sum(o => o.TotalAmount) ?? 0;
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{c.FullName}</td>");
            sb.AppendLine($"<td>{c.Phone ?? "-"}</td>");
            sb.AppendLine($"<td>{c.Email ?? "-"}</td>");
            sb.AppendLine($"<td>{c.Discount}%</td>");
            sb.AppendLine($"<td>{c.RegistrationDate:dd.MM.yyyy}</td>");
            sb.AppendLine($"<td>{orderCount}</td>");
            sb.AppendLine($"<td>{orderSum:N0} ₽</td>");
            sb.AppendLine($"</tr>");
            totalSpent += orderSum;
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine($"<tfoot><tr class='total'><td colspan='4' style='text-align:right;'>Общая сумма заказов:</td><td colspan='3'>{totalSpent:N0} ₽</td></tr></tfoot>");
        sb.AppendLine("</table>");
    }

    private async Task BuildWarehouseHtml(StringBuilder sb)
    {
        var warehouses = await _context.Warehouses
            .Include(w => w.Products)
            .OrderBy(w => w.Name)
            .ToListAsync();

        if (!warehouses.Any())
        {
            sb.AppendLine("<p>Нет данных о складах</p>");
            return;
        }

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Название</th><th>Местоположение</th><th>Вместимость</th><th>Текущая загрузка</th><th>Загрузка %</th><th>Кол-во товаров</th><th>Стоимость товаров</th></tr></thead>");
        sb.AppendLine("<tbody>");

        int totalCapacity = 0;
        int totalOccupancy = 0;
        decimal totalValue = 0;

        foreach (var w in warehouses)
        {
            var occupancy = w.Products?.Sum(p => p.CurrentStock) ?? 0;
            var percent = w.Capacity > 0 ? occupancy * 100.0 / w.Capacity : 0;
            var stockValue = w.Products?.Sum(p => p.CurrentStock * p.PurchasePrice) ?? 0;

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{w.Name}</td>");
            sb.AppendLine($"<td>{w.Location ?? "-"}</td>");
            sb.AppendLine($"<td>{w.Capacity}</td>");
            sb.AppendLine($"<td>{occupancy}</td>");
            sb.AppendLine($"<td>{percent:F1}%</td>");
            sb.AppendLine($"<td>{w.Products?.Count ?? 0}</td>");
            sb.AppendLine($"<td>{stockValue:N0} ₽</td>");
            sb.AppendLine($"</tr>");

            totalCapacity += w.Capacity;
            totalOccupancy += occupancy;
            totalValue += stockValue;
        }

        sb.AppendLine("</tbody>");
        var totalPercent = totalCapacity > 0 ? totalOccupancy * 100.0 / totalCapacity : 0;
        sb.AppendLine($"<tfoot><tr class='total'><td colspan='2' style='text-align:right;'>ИТОГО:</td><td>{totalCapacity}</td><td>{totalOccupancy}</td><td>{totalPercent:F1}%</td><td></td><td>{totalValue:N0} ₽</td></tr></tfoot>");
        sb.AppendLine("</table>");
    }

    #endregion

    #region Helper Methods

    private string GetReportName(string type) => type switch
    {
        "sales" => "продажам",
        "products" => "товарам",
        "customers" => "клиентам",
        "warehouse" => "складам",
        _ => type
    };

    private string GetStatusText(string? status) => status switch
    {
        "Completed" => "Выполнен",
        "Pending" => "Ожидание",
        "Cancelled" => "Отменён",
        _ => status ?? "-"
    };

    private string GetPaymentText(string? method) => method switch
    {
        "Card" => "Карта",
        "Cash" => "Наличные",
        "Credit" => "Кредит",
        _ => method ?? "-"
    };

    #endregion
}