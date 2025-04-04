using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using GrillHouseNNProg.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection.Metadata;


namespace GrillHouseNNProg.ViewModels
{
    public class OrderHistoryScreenViewModel : ViewModelBase
    {
        private readonly GrillcitynnContext _db;
        private Order _ordert;
        private ObservableCollection<Order> _orders;
        private DateTime _startOrderDate = DateTime.Today.AddDays(-30);
        private DateTime _endOrderDate = DateTime.Today;

        public DateTimeOffset StartOrderDate
        {
            get => new DateTimeOffset(_startOrderDate, TimeZoneInfo.Local.GetUtcOffset(_startOrderDate));
            set
            {
                var newDate = DateTime.SpecifyKind(new DateTime(value.Year, value.Month, value.Day), DateTimeKind.Local);
                this.RaiseAndSetIfChanged(ref _startOrderDate, newDate);
                LoadOrders();
            }
        }

        public DateTimeOffset EndOrderDate
        {
            get => new DateTimeOffset(_endOrderDate, TimeZoneInfo.Local.GetUtcOffset(_endOrderDate));
            set
            {
                var newDate = DateTime.SpecifyKind(new DateTime(value.Year, value.Month, value.Day), DateTimeKind.Local);
                this.RaiseAndSetIfChanged(ref _endOrderDate, newDate);
                LoadOrders();
            }
        }


        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ExportToPdfCommand { get; }


        public Order Ordert
        {
            get => _ordert;
            set => this.RaiseAndSetIfChanged(ref _ordert, value);
        }

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => this.RaiseAndSetIfChanged(ref _orders, value);
        }

        //private void LoadOrders()
        //{
        //    var ordersFromDb = _db.Orders
        //        .Include(x => x.Product)
        //        .ThenInclude(p => p.Provider)
        //        .Include(x => x.Discount)
        //        .ToList();

        //    Orders = new ObservableCollection<Order>(ordersFromDb);
        //}

        private void LoadOrders()
        {
            // Конвертируем DateTime в DateOnly для сравнения с полем DateOfOrder
            var startDate = DateOnly.FromDateTime(_startOrderDate);
            var endDate = DateOnly.FromDateTime(_endOrderDate.AddDays(1)); // Добавляем день, чтобы включить всю конечную дату

            var ordersFromDb = _db.Orders
                .Include(x => x.Product)
                .ThenInclude(p => p.Provider)
                .Include(x => x.Discount)
                .Where(order => order.DateOfOrder != null &&
                       order.DateOfOrder >= startDate &&
                       order.DateOfOrder <= endDate)
                .ToList();

            Orders = new ObservableCollection<Order>(ordersFromDb);
        }

        public OrderHistoryScreenViewModel(GrillcitynnContext db)
        {
            _db = db;

            // Устанавливаем начальный период - последние 30 дней
            _startOrderDate = DateTime.Today.AddDays(-30);
            _endOrderDate = DateTime.Today;

            LoadOrders();
            QuestPDF.Settings.License = LicenseType.Community;
            ExportToPdfCommand = ReactiveCommand.Create(ExportToPdf);
        }

        //private void ExportToPdf()
        //{
        //    string filePath = "Отчет_продаж.pdf";

        //    Группировка заказов по поставщикам и подсчет общей выручки
        //    var providerRevenue = Orders
        //        .Where(order => order.Product?.Provider != null)
        //        .GroupBy(order => order.Product.Provider.ProviderName)
        //        .Select(group => new
        //        {
        //            ProviderName = group.Key ?? "Неизвестный поставщик",
        //            TotalRevenue = group.Sum(order => order.FinalPrice)
        //        })
        //        .OrderByDescending(x => x.TotalRevenue) // Сортируем по убыванию выручки
        //        .ToList();

        //    QuestPDF.Fluent.Document.Create(container =>
        //    {
        //        container.Page(page =>
        //        {
        //            page.Size(PageSizes.A4);
        //            page.Margin(20);
        //            page.DefaultTextStyle(x => x.FontFamily("Arial"));

        //            Заголовок отчета
        //            page.Header().AlignCenter().Text("Отчет по продажам")
        //                .FontSize(20).SemiBold();

        //            Основная таблица заказов
        //            page.Content().Table(table =>
        //            {
        //                table.ColumnsDefinition(columns =>
        //                {
        //                    columns.ConstantColumn(100);
        //                    columns.RelativeColumn();
        //                    columns.ConstantColumn(100);
        //                    columns.ConstantColumn(120);
        //                    columns.ConstantColumn(100);
        //                });

        //                table.Header(header =>
        //                {
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Дата").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Товар").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Скидка").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Поставщик").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Цена").Bold();
        //                });

        //                foreach (var order in Orders)
        //                {
        //                    table.Cell().Padding(5).Text(order.DateOfOrder?.ToString("dd.MM.yyyy") ?? "—");
        //                    table.Cell().Padding(5).Text(order.Product?.ProductName ?? "Неизвестно");
        //                    table.Cell().Padding(5).Text(order.Discount?.DiscountPercent != null ? $"{order.Discount.DiscountPercent}%" : "0%");
        //                    table.Cell().Padding(5).Text(order.Product?.Provider?.ProviderName ?? "Неизвестно");
        //                    table.Cell().Padding(5).Text($"{order.FinalPrice} руб.");
        //                }
        //            });

        //            page.Footer().AlignCenter().Text($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm}");
        //        });

        //        Страница статистики по поставщикам
        //        container.Page(page =>
        //        {
        //            page.Size(PageSizes.A4);
        //            page.Margin(20);
        //            page.DefaultTextStyle(x => x.FontFamily("Arial"));

        //            page.Header().AlignCenter().Text("Статистика по поставщикам")
        //                .FontSize(18).SemiBold();

        //            page.Content().Table(table =>
        //            {
        //                table.ColumnsDefinition(columns =>
        //                {
        //                    columns.RelativeColumn();
        //                    columns.ConstantColumn(120);
        //                });

        //                table.Header(header =>
        //                {
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Поставщик").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Общая выручка").Bold();
        //                });

        //                foreach (var provider in providerRevenue)
        //                {
        //                    table.Cell().Padding(5).Text(provider.ProviderName);
        //                    table.Cell().Padding(5).Text($"{provider.TotalRevenue} руб.");
        //                }
        //            });

        //            page.Footer().AlignCenter().Text($"Дата генерации: {DateTime.Now:dd.MM.yyyy HH:mm}");
        //        });
        //    }).GeneratePdf(filePath);

        //    Открыть файл после генерации(опционально)
        //    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        //    {
        //        FileName = filePath,
        //        UseShellExecute = true
        //    });
        //}

        private void ExportToPdf()
        {
            // Добавляем даты периода в название файла
            string filePath = $"Отчет_продаж_{_startOrderDate:ddMMyyyy}_{_endOrderDate:ddMMyyyy}.pdf";

            // Фильтруем заказы по выбранному периоду (как в LoadOrders)
            var filteredOrders = _db.Orders
                .Include(x => x.Product)
                .ThenInclude(p => p.Provider)
                .Include(x => x.Discount)
                .Where(order => order.DateOfOrder != null &&
                       order.DateOfOrder >= DateOnly.FromDateTime(_startOrderDate) &&
                       order.DateOfOrder <= DateOnly.FromDateTime(_endOrderDate.AddDays(1)))
                .ToList();

            // Группировка отфильтрованных заказов по поставщикам
            var providerRevenue = filteredOrders
                .Where(order => order.Product?.Provider != null)
                .GroupBy(order => order.Product.Provider.ProviderName)
                .Select(group => new
                {
                    ProviderName = group.Key ?? "Неизвестный поставщик",
                    TotalRevenue = group.Sum(order => order.FinalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    // Заголовок с указанием периода
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("Отчет по продажам").FontSize(20).SemiBold();
                        column.Item().AlignCenter().Text($"за период с {_startOrderDate:dd.MM.yyyy} по {_endOrderDate:dd.MM.yyyy}").FontSize(14);
                    });

                    // Основная таблица заказов
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(100);
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(120);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Дата").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Товар").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Скидка").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Поставщик").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Цена").Bold();
                        });

                        foreach (var order in filteredOrders)
                        {
                            table.Cell().Padding(5).Text(order.DateOfOrder?.ToString("dd.MM.yyyy") ?? "—");
                            table.Cell().Padding(5).Text(order.Product?.ProductName ?? "Неизвестно");
                            table.Cell().Padding(5).Text(order.Discount?.DiscountPercent != null ? $"{order.Discount.DiscountPercent}%" : "0%");
                            table.Cell().Padding(5).Text(order.Product?.Provider?.ProviderName ?? "Неизвестно");
                            table.Cell().Padding(5).Text($"{order.FinalPrice} руб.");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Дата создания: ");
                        x.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                    });
                });

                // Страница статистики по поставщикам
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("Статистика по поставщикам").FontSize(18).SemiBold();
                        column.Item().AlignCenter().Text($"за период с {_startOrderDate:dd.MM.yyyy} по {_endOrderDate:dd.MM.yyyy}").FontSize(12);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(120);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Поставщик").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Общая выручка").Bold();
                        });

                        foreach (var provider in providerRevenue)
                        {
                            table.Cell().Padding(5).Text(provider.ProviderName);
                            table.Cell().Padding(5).Text($"{provider.TotalRevenue} руб.");
                        }

                        // Итоговая строка
                        table.Cell().ColumnSpan(2).Padding(5).AlignRight().Text($"Итого: {providerRevenue.Sum(x => x.TotalRevenue)} руб.").Bold();
                    });

                    page.Footer().AlignCenter().Text($"Дата генерации: {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            }).GeneratePdf(filePath);

            // Открыть файл после генерации
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }

    }
}
