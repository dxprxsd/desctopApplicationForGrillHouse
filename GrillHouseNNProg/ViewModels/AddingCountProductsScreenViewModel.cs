using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GrillHouseNNProg.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReactiveUI;
using GrillHouseNNProg.Resources;

namespace GrillHouseNNProg.ViewModels
{
    public class AddingCountProductsScreenViewModel : ViewModelBase
    {
        private readonly GrillcitynnContext _db;
        private Product _selectedProduct;
        private ObservableCollection<Product> _products;
        private static Dictionary<int, int> _initialStock; // Начальное количество товаров (статическое)
        private static Dictionary<int, int> _receivedStock = new(); // Сделали статическим, чтобы данные сохранялись при переходе на другие экраны
        private int _enteredQuantity; // Для хранения введенного количества товара
        private string _errorMessage;
        private DateTime _startDate = DateTime.Today.AddDays(-7);
        private DateTime _endDate = DateTime.Now;

        public DateTimeOffset StartDate
        {
            get => new DateTimeOffset(_startDate, TimeZoneInfo.Local.GetUtcOffset(_startDate));
            set => this.RaiseAndSetIfChanged(ref _startDate, DateTime.SpecifyKind(new DateTime(value.Year, value.Month, value.Day), DateTimeKind.Local));
        }

        public DateTimeOffset EndDate
        {
            get => new DateTimeOffset(_endDate, TimeZoneInfo.Local.GetUtcOffset(_endDate));
            set => this.RaiseAndSetIfChanged(ref _endDate, DateTime.SpecifyKind(new DateTime(value.Year, value.Month, value.Day), DateTimeKind.Local));
        }


        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> UpdateProductStockCommand { get; }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => this.RaiseAndSetIfChanged(ref _products, value);
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set => this.RaiseAndSetIfChanged(ref _selectedProduct, value);
        }

        public int EnteredQuantity
        {
            get => _enteredQuantity;
            set => this.RaiseAndSetIfChanged(ref _enteredQuantity, value);
        }

        public AddingCountProductsScreenViewModel(GrillcitynnContext db)
        {
            _db = db;

            if (_initialStock == null) // Загружаем данные при первом запуске
            {
                LoadInitialStock();
            }

            LoadProducts();
            UpdateProductStockCommand = ReactiveCommand.Create(UpdateProductStock);
        }

        private void LoadInitialStock()
        {
            _initialStock = _db.Products.ToDictionary(p => p.Id, p => p.QuantityInStock ?? 0);

            // Загружаем добавленные товары из базы (если они были сохранены)
            foreach (var product in _db.Products)
            {
                if (!_receivedStock.ContainsKey(product.Id))
                {
                    _receivedStock[product.Id] = 0;
                }
            }
        }

        private void LoadProducts()
        {
            Products = new ObservableCollection<Product>(_db.Products.ToList());
        }

        /// <summary>
        /// Обновляет количество товара с учетом поступлений и сохраняет в БД.
        /// </summary>
        private void UpdateProductStock()
        {
            if (SelectedProduct != null && EnteredQuantity > 0)
            {
                if (!_receivedStock.ContainsKey(SelectedProduct.Id))
                {
                    _receivedStock[SelectedProduct.Id] = 0;
                }

                _receivedStock[SelectedProduct.Id] += EnteredQuantity;

                var product = _db.Products.FirstOrDefault(p => p.Id == SelectedProduct.Id);
                if (product != null)
                {
                    product.QuantityInStock = (product.QuantityInStock ?? 0) + EnteredQuantity;
                    QuestPDF.Settings.License = LicenseType.Community;
                    _db.Products.Update(product);
                    _db.SaveChanges(); // Сохраняем изменения сразу
                }
                ErrorMessage = "Готово!";
            }
        }

        private List<ProductMovement> GetProductMovementsInRange()
        {
            DateOnly startDateOnly = DateOnly.FromDateTime(_startDate);
            DateOnly endDateOnly = DateOnly.FromDateTime(_endDate);

            return _db.ProductMovements
                .Where(pm => pm.MovementDate.HasValue &&
                             pm.MovementDate.Value >= startDateOnly &&
                             pm.MovementDate.Value <= endDateOnly)
                .ToList();
        }

        public void ExportToPdf()
        {
            string filePath = "Отчет_по_движению_товаров.pdf";
            var movements = GetProductMovementsInRange();

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Header().AlignCenter().Text("Отчет о движении товаров")
                        .FontSize(20).SemiBold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();  // Название товара
                            columns.ConstantColumn(80); // Приход
                            columns.ConstantColumn(80); // Продажа
                            columns.ConstantColumn(80); // Остаток
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Товар").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Приход").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Продажа").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Остаток").Bold();
                        });

                        foreach (var product in Products)
                        {
                            int received = movements
                                .Where(m => m.ProductId == product.Id && m.MovementType == "incoming")
                                .Sum(m => m.Quantity);

                            int sold = movements
                                .Where(m => m.ProductId == product.Id && m.MovementType == "sale")
                                .Sum(m => m.Quantity);

                            int stock = product.QuantityInStock ?? 0;

                            table.Cell().Padding(5).Text(product.ProductName);
                            table.Cell().Padding(5).Text(received > 0 ? $"+{received}" : "-");
                            table.Cell().Padding(5).Text(sold > 0 ? $"-{sold}" : "-");
                            table.Cell().Padding(5).Text(stock.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text($"Дата генерации: {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            }).GeneratePdf(filePath);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
    }
}