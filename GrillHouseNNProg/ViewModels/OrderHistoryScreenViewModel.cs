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
            // ������������ DateTime � DateOnly ��� ��������� � ����� DateOfOrder
            var startDate = DateOnly.FromDateTime(_startOrderDate);
            var endDate = DateOnly.FromDateTime(_endOrderDate.AddDays(1)); // ��������� ����, ����� �������� ��� �������� ����

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

            // ������������� ��������� ������ - ��������� 30 ����
            _startOrderDate = DateTime.Today.AddDays(-30);
            _endOrderDate = DateTime.Today;

            LoadOrders();
            QuestPDF.Settings.License = LicenseType.Community;
            ExportToPdfCommand = ReactiveCommand.Create(ExportToPdf);
        }

        //private void ExportToPdf()
        //{
        //    string filePath = "�����_������.pdf";

        //    ����������� ������� �� ����������� � ������� ����� �������
        //    var providerRevenue = Orders
        //        .Where(order => order.Product?.Provider != null)
        //        .GroupBy(order => order.Product.Provider.ProviderName)
        //        .Select(group => new
        //        {
        //            ProviderName = group.Key ?? "����������� ���������",
        //            TotalRevenue = group.Sum(order => order.FinalPrice)
        //        })
        //        .OrderByDescending(x => x.TotalRevenue) // ��������� �� �������� �������
        //        .ToList();

        //    QuestPDF.Fluent.Document.Create(container =>
        //    {
        //        container.Page(page =>
        //        {
        //            page.Size(PageSizes.A4);
        //            page.Margin(20);
        //            page.DefaultTextStyle(x => x.FontFamily("Arial"));

        //            ��������� ������
        //            page.Header().AlignCenter().Text("����� �� ��������")
        //                .FontSize(20).SemiBold();

        //            �������� ������� �������
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
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("����").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("�����").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("������").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("���������").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("����").Bold();
        //                });

        //                foreach (var order in Orders)
        //                {
        //                    table.Cell().Padding(5).Text(order.DateOfOrder?.ToString("dd.MM.yyyy") ?? "�");
        //                    table.Cell().Padding(5).Text(order.Product?.ProductName ?? "����������");
        //                    table.Cell().Padding(5).Text(order.Discount?.DiscountPercent != null ? $"{order.Discount.DiscountPercent}%" : "0%");
        //                    table.Cell().Padding(5).Text(order.Product?.Provider?.ProviderName ?? "����������");
        //                    table.Cell().Padding(5).Text($"{order.FinalPrice} ���.");
        //                }
        //            });

        //            page.Footer().AlignCenter().Text($"���� ��������: {DateTime.Now:dd.MM.yyyy HH:mm}");
        //        });

        //        �������� ���������� �� �����������
        //        container.Page(page =>
        //        {
        //            page.Size(PageSizes.A4);
        //            page.Margin(20);
        //            page.DefaultTextStyle(x => x.FontFamily("Arial"));

        //            page.Header().AlignCenter().Text("���������� �� �����������")
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
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("���������").Bold();
        //                    header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("����� �������").Bold();
        //                });

        //                foreach (var provider in providerRevenue)
        //                {
        //                    table.Cell().Padding(5).Text(provider.ProviderName);
        //                    table.Cell().Padding(5).Text($"{provider.TotalRevenue} ���.");
        //                }
        //            });

        //            page.Footer().AlignCenter().Text($"���� ���������: {DateTime.Now:dd.MM.yyyy HH:mm}");
        //        });
        //    }).GeneratePdf(filePath);

        //    ������� ���� ����� ���������(�����������)
        //    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        //    {
        //        FileName = filePath,
        //        UseShellExecute = true
        //    });
        //}

        private void ExportToPdf()
        {
            // ��������� ���� ������� � �������� �����
            string filePath = $"�����_������_{_startOrderDate:ddMMyyyy}_{_endOrderDate:ddMMyyyy}.pdf";

            // ��������� ������ �� ���������� ������� (��� � LoadOrders)
            var filteredOrders = _db.Orders
                .Include(x => x.Product)
                .ThenInclude(p => p.Provider)
                .Include(x => x.Discount)
                .Where(order => order.DateOfOrder != null &&
                       order.DateOfOrder >= DateOnly.FromDateTime(_startOrderDate) &&
                       order.DateOfOrder <= DateOnly.FromDateTime(_endOrderDate.AddDays(1)))
                .ToList();

            // ����������� ��������������� ������� �� �����������
            var providerRevenue = filteredOrders
                .Where(order => order.Product?.Provider != null)
                .GroupBy(order => order.Product.Provider.ProviderName)
                .Select(group => new
                {
                    ProviderName = group.Key ?? "����������� ���������",
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

                    // ��������� � ��������� �������
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("����� �� ��������").FontSize(20).SemiBold();
                        column.Item().AlignCenter().Text($"�� ������ � {_startOrderDate:dd.MM.yyyy} �� {_endOrderDate:dd.MM.yyyy}").FontSize(14);
                    });

                    // �������� ������� �������
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
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("����").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("�����").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("������").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("���������").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("����").Bold();
                        });

                        foreach (var order in filteredOrders)
                        {
                            table.Cell().Padding(5).Text(order.DateOfOrder?.ToString("dd.MM.yyyy") ?? "�");
                            table.Cell().Padding(5).Text(order.Product?.ProductName ?? "����������");
                            table.Cell().Padding(5).Text(order.Discount?.DiscountPercent != null ? $"{order.Discount.DiscountPercent}%" : "0%");
                            table.Cell().Padding(5).Text(order.Product?.Provider?.ProviderName ?? "����������");
                            table.Cell().Padding(5).Text($"{order.FinalPrice} ���.");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("���� ��������: ");
                        x.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                    });
                });

                // �������� ���������� �� �����������
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("���������� �� �����������").FontSize(18).SemiBold();
                        column.Item().AlignCenter().Text($"�� ������ � {_startOrderDate:dd.MM.yyyy} �� {_endOrderDate:dd.MM.yyyy}").FontSize(12);
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
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("���������").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("����� �������").Bold();
                        });

                        foreach (var provider in providerRevenue)
                        {
                            table.Cell().Padding(5).Text(provider.ProviderName);
                            table.Cell().Padding(5).Text($"{provider.TotalRevenue} ���.");
                        }

                        // �������� ������
                        table.Cell().ColumnSpan(2).Padding(5).AlignRight().Text($"�����: {providerRevenue.Sum(x => x.TotalRevenue)} ���.").Bold();
                    });

                    page.Footer().AlignCenter().Text($"���� ���������: {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            }).GeneratePdf(filePath);

            // ������� ���� ����� ���������
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }

    }
}
