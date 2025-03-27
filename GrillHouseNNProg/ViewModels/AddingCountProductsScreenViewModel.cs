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

        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> UpdateProductStockCommand { get; }

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
            }
        }

        public void ExportToPdf()
        {
            string filePath = "Отчет_по_товарам.pdf";

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Header().AlignCenter().Text("Отчет по товарам")
                        .FontSize(20).SemiBold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();  // Название товара
                            columns.ConstantColumn(80); // Начальное количество
                            columns.ConstantColumn(80); // Приход
                            columns.ConstantColumn(80); // Продажа
                            columns.ConstantColumn(80); // Итоговое количество
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Товар").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Начало").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Приход").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Продажа").Bold();
                            header.Cell().BorderBottom(1).BorderColor("#000").Padding(5).Text("Остаток").Bold();
                        });

                        foreach (var product in Products)
                        {
                            int initial = _initialStock.ContainsKey(product.Id) ? _initialStock[product.Id] : 0;
                            int current = product.QuantityInStock ?? 0;
                            int received = _receivedStock.ContainsKey(product.Id) ? _receivedStock[product.Id] : 0;
                            int sold = SoldStockTracker.GetSoldQuantity(product.Id);

                            table.Cell().Padding(5).Text(product.ProductName);
                            table.Cell().Padding(5).Text(initial.ToString());
                            table.Cell().Padding(5).Text(received > 0 ? $"+{received}" : "-");
                            table.Cell().Padding(5).Text(sold > 0 ? $"-{sold}" : "-");
                            table.Cell().Padding(5).Text(current.ToString());
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