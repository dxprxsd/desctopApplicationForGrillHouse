using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using ReactiveUI;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using GrillHouseNNProg.Models;
using GrillHouseNNProg.Resources;
using System.Threading.Tasks;
using Unit = System.Reactive.Unit;
using System.Linq;

namespace GrillHouseNNProg.ViewModels
{
    public class OrderHistoryScreenViewModel : ViewModelBase
    {
        private readonly ApiService _apiService;
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

        public async void LoadOrders()
        {
            try
            {
                var allOrders = await _apiService.GetAllOrdersAsync();

                var start = DateOnly.FromDateTime(_startOrderDate);
                var end = DateOnly.FromDateTime(_endOrderDate);

                var filtered = allOrders
                    .Where(o => o.DateOfOrder.HasValue &&
                                o.DateOfOrder.Value >= start &&
                                o.DateOfOrder.Value <= end)
                    .ToList();

                Orders = new ObservableCollection<Order>(filtered);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading orders: {ex.Message}");
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