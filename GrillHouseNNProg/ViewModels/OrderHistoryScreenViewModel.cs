using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive;
using System.Collections.Generic;
using GrillHouseNNProg.Models;
using GrillHouseNNProg.Resources;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Unit = System.Reactive.Unit;
using QuestPDF.Helpers;

namespace GrillHouseNNProg.ViewModels
{
    public class OrderHistoryScreenViewModel : ViewModelBase
    {
        private readonly ApiService _apiService;
        private Order _ordert;
        private ObservableCollection<Order> _orders;
        private DateTime _startOrderDate = DateTime.Today.AddDays(-30);
        private DateTime _endOrderDate = DateTime.Today;

        private List<ISeries> _pieSeries; // Заменяем на List<ISeries>
        public List<ISeries> PieSeries
        {
            get => _pieSeries;
            set => this.RaiseAndSetIfChanged(ref _pieSeries, value);
        }


        private string[] _labels;
        public string[] Labels
        {
            get => _labels;
            set => this.RaiseAndSetIfChanged(ref _labels, value);
        }

        private ObservableCollection<Provider> _providers;
        public ObservableCollection<Provider> Providers
        {
            get => _providers;
            set => this.RaiseAndSetIfChanged(ref _providers, value);
        }


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

        public ReactiveCommand<Unit, Unit> ExportToPdfCommand { get; }

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

        //public async void LoadOrders()
        //{
        //    try
        //    {
        //        var allOrders = await _apiService.GetAllOrdersAsync();
        //        var filtered = allOrders
        //            .Where(o => o.DateOfOrder.HasValue &&
        //                        o.DateOfOrder.Value >= DateOnly.FromDateTime(_startOrderDate) &&
        //                        o.DateOfOrder.Value <= DateOnly.FromDateTime(_endOrderDate))
        //            .ToList();

        //        Orders = new ObservableCollection<Order>(filtered);
        //        UpdateChartData(filtered); // обновляем диаграмму
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error loading orders: {ex.Message}");
        //    }
        //}

        public async void LoadOrders()
        {
            try
            {
                var allOrders = await _apiService.GetAllOrdersAsync();
                var filtered = allOrders
                    .Where(o => o.DateOfOrder.HasValue &&
                                o.DateOfOrder.Value >= DateOnly.FromDateTime(_startOrderDate) &&
                                o.DateOfOrder.Value <= DateOnly.FromDateTime(_endOrderDate) &&
                                o.Product != null && o.Product.Provider != null) // Ensure Product and Provider are not null
                    .ToList();

                // Сортировка и группировка поставщиков по сумме заказов
                var providerGroups = filtered
                    .GroupBy(o => o.Product.Provider)
                    .Select(g => new
                    {
                        Provider = g.Key,
                        TotalAmount = g.Sum(o => o.FinalPrice) // Сумма заказов для каждого поставщика
                    })
                    .OrderByDescending(x => x.TotalAmount) // Сортировка по убыванию суммы заказов
                    .ToList();

                // Обновляем список поставщиков
                var providers = providerGroups.Select(pg => pg.Provider).ToList();
                Providers = new ObservableCollection<Provider>(providers); // Используем свойство Providers

                Orders = new ObservableCollection<Order>(filtered);
                UpdateChartData(filtered); // обновляем диаграмму
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading orders: {ex.Message}");
            }
        }

        public void UpdateChartData(List<Order> orders)
        {
            try
            {
                var providerGroups = orders
                    .Where(o => o.Product?.Provider?.ProviderName != null) // Ensure ProviderName is not null
                    .GroupBy(o => o.Product?.Provider?.ProviderName ?? "Неизвестный поставщик")
                    .Select(g => new { Provider = g.Key, Count = g.Count(), TotalAmount = g.Sum(o => o.FinalPrice) })
                    .OrderByDescending(g => g.TotalAmount) // Сортировка по сумме заказов
                    .ToList();

                var series = providerGroups
                    .Select(g => new PieSeries<double>
                    {
                        Values = new[] { (double)g.TotalAmount },
                        Name = g.Provider,
                        DataLabelsSize = 16,
                        DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.ChartCenter,
                        Fill = new SolidColorPaint(new SKColor(
                            (byte)Random.Shared.Next(256),
                            (byte)Random.Shared.Next(256),
                            (byte)Random.Shared.Next(256)))
                    }).ToList<ISeries>();

                PieSeries = new List<ISeries>(series);
                this.RaisePropertyChanged(nameof(PieSeries));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateChartData: {ex.Message}");
            }
        }




        public OrderHistoryScreenViewModel(ApiService apiService)
        {
            _apiService = apiService;

            _startOrderDate = DateTime.Today.AddDays(-30);
            _endOrderDate = DateTime.Today;

            LoadOrders();
            QuestPDF.Settings.License = LicenseType.Community;
            ExportToPdfCommand = ReactiveCommand.CreateFromTask(ExportToPdfAsync);
        }

        public async Task ExportToPdfAsync()
        {
            try
            {
                string filePath = $"Отчет_продаж_{_startOrderDate:ddMMyyyy}_{_endOrderDate:ddMMyyyy}.pdf";
                var allOrders = await _apiService.GetAllOrdersAsync();

                var start = DateOnly.FromDateTime(_startOrderDate);
                var end = DateOnly.FromDateTime(_endOrderDate);

                var filteredOrders = allOrders
                    .Where(o => o.DateOfOrder.HasValue &&
                                o.DateOfOrder.Value >= start &&
                                o.DateOfOrder.Value <= end)
                    .ToList();

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontFamily("Arial"));

                        page.Header().Column(column =>
                        {
                            column.Item().AlignCenter().Text("Отчет по продажам").FontSize(20).SemiBold();
                            column.Item().AlignCenter().Text($"за период с {_startOrderDate:dd.MM.yyyy} по {_endOrderDate:dd.MM.yyyy}").FontSize(14);
                        });

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
                }).GeneratePdf(filePath);

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex.Message}");
            }
        }


    }
}