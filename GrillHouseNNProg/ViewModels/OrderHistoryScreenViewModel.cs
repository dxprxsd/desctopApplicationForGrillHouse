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

        private void LoadOrders()
        {
            var ordersFromDb = _db.Orders
                .Include(x => x.Product)
                .ThenInclude(p => p.Provider)
                .Include(x => x.Discount)
                .ToList();

            Orders = new ObservableCollection<Order>(ordersFromDb);
        }

        public OrderHistoryScreenViewModel(GrillcitynnContext db)
        {
            _db = db;
            LoadOrders();
            QuestPDF.Settings.License = LicenseType.Community;
            ExportToPdfCommand = ReactiveCommand.Create(ExportToPdf);
        }

        private void ExportToPdf()
        {
            string filePath = "Отчет_продаж.pdf";

            // Группировка заказов по поставщикам и подсчет общей выручки
            var providerRevenue = Orders
                .Where(order => order.Product?.Provider != null)
                .GroupBy(order => order.Product.Provider.ProviderName)
                .Select(group => new
                {
                    ProviderName = group.Key ?? "Неизвестный поставщик",
                    TotalRevenue = group.Sum(order => order.FinalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue) // Сортируем по убыванию выручки
                .ToList();

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    // Заголовок отчета
                    page.Header().AlignCenter().Text("Отчет по продажам")
                        .FontSize(20).SemiBold();

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

                        foreach (var order in Orders)
                        {
                            table.Cell().Padding(5).Text(order.DateOfOrder?.ToString("dd.MM.yyyy") ?? "—");
                            table.Cell().Padding(5).Text(order.Product?.ProductName ?? "Неизвестно");
                            table.Cell().Padding(5).Text(order.Discount?.DiscountPercent != null ? $"{order.Discount.DiscountPercent}%" : "0%");
                            table.Cell().Padding(5).Text(order.Product?.Provider?.ProviderName ?? "Неизвестно");
                            table.Cell().Padding(5).Text($"{order.FinalPrice} руб.");
                        }
                    });

                    page.Footer().AlignCenter().Text($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm}");
                });

                // Страница статистики по поставщикам
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Header().AlignCenter().Text("Статистика по поставщикам")
                        .FontSize(18).SemiBold();

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
                    });

                    page.Footer().AlignCenter().Text($"Дата генерации: {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            }).GeneratePdf(filePath);

            // Открыть файл после генерации (опционально)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
    }
}
