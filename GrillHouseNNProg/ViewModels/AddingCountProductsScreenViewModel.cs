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
using System.Threading.Tasks;
using System.Reactive;
using System.Diagnostics;
using System.Net.Http;
using System.IO;

namespace GrillHouseNNProg.ViewModels
{
    public class AddingCountProductsScreenViewModel : ViewModelBase
    {
        private readonly ApiService _apiService; // Заменяем прямой доступ к БД на ApiService
        private readonly GrillcitynnContext _db;
        private Product _selectedProduct;
        private ObservableCollection<Product> _products;
        private static Dictionary<int, int> _initialStock; // Начальное количество товаров (статическое)
        private static Dictionary<int, int> _receivedStock = new(); // Сделали статическим, чтобы данные сохранялись при переходе на другие экраны
        private int _enteredQuantity; // Для хранения введенного количества товара
        private string _errorMessage;
        private DateTime _startDate = DateTime.Today.AddDays(-7);
        private DateTime _endDate = DateTime.Now;
        private ObservableCollection<ProductMovement> _productMovements; // Добавляем коллекцию для хранения движений товаров

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

        // Новая коллекция для хранения движений товаров
        public ObservableCollection<ProductMovement> ProductMovements
        {
            get => _productMovements;
            set => this.RaiseAndSetIfChanged(ref _productMovements, value);
        }

        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> UpdateProductStockCommand { get; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> LoadMovementsCommand { get; }

        //public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> UpdateProductStockCommand { get; }

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

        public AddingCountProductsScreenViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            //_db = db;

            QuestPDF.Settings.License = LicenseType.Community;
            _ = LoadProductsAsync(); // Асинхронная загрузка продуктов
            //UpdateProductStockCommand = ReactiveCommand.Create(UpdateProductStock);
            UpdateProductStockCommand = ReactiveCommand.CreateFromTask(UpdateProductStockAsync);
            LoadMovementsCommand = ReactiveCommand.CreateFromTask(LoadMovementsAsync);
        }

        public async Task LoadInitialStockAsync()
        {
            try
            {
                var products = await _apiService.GetAllProductsAsync();
                _initialStock = products.ToDictionary(p => p.Id, p => p.QuantityInStock ?? 0);

                foreach (var product in products)
                {
                    if (!_receivedStock.ContainsKey(product.Id))
                    {
                        _receivedStock[product.Id] = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки начальных данных: {ex.Message}";
            }
        }

        public async Task LoadProductsAsync()
        {
            try
            {
                var products = await _apiService.GetAllProductsAsync();
                Products = new ObservableCollection<Product>(products);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки продуктов: {ex.Message}";
                Debug.WriteLine(ex);
                throw; // Пробрасываем исключение дальше для обработки в InitializeDataAsync
            }
        }

        public async Task UpdateProductStockAsync()
        {
            if (SelectedProduct == null || EnteredQuantity <= 0)
            {
                ErrorMessage = "Выберите товар и укажите положительное количество";
                return;
            }

            try
            {
                // Логируем перед отправкой
                Debug.WriteLine($"Отправка запроса: ProductId={SelectedProduct.Id}, Quantity={EnteredQuantity}");

                // Обновляем на сервере
                var result = await _apiService.UpdateProductStockAsync(
                    SelectedProduct.Id,
                    EnteredQuantity);

                // Обновляем локальные данные
                _receivedStock[SelectedProduct.Id] = _receivedStock.GetValueOrDefault(SelectedProduct.Id, 0) + EnteredQuantity;

                // Обновляем список продуктов
                await LoadProductsAsync();

                ErrorMessage = "Количество товара успешно обновлено!";
            }
            catch (HttpRequestException httpEx)
            {
                ErrorMessage = $"Ошибка сети: {httpEx.Message}";
                Debug.WriteLine(httpEx);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка: {ex.Message}";
                Debug.WriteLine(ex);
            }
        }


        private async Task LoadMovementsAsync()
        {
            try
            {
                var allMovements = await _apiService.GetProductMovementsAsync(DateTime.MinValue, DateTime.MaxValue);

                // Фильтрация на клиенте
                var filtered = allMovements?
                    .Where(m => m.MovementDate >= _startDate && m.MovementDate <= _endDate)
                    .ToList();

                // Обработка данных...
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки: {ex.Message}";
                Debug.WriteLine(ex);
            }
        }

        public async Task ExportToPdf()
        {
            try
            {
                // Получаем все движения товаров
                var allMovements = await _apiService.GetProductMovementsAsync(DateTime.MinValue, DateTime.MaxValue);

                // Фильтруем по дате на стороне клиента
                var filteredMovements = allMovements?
                    .Where(m => m.MovementDate >= _startDate && m.MovementDate <= _endDate)
                    .ToList();

                if (filteredMovements == null || !filteredMovements.Any())
                {
                    ErrorMessage = "Нет данных о движении товаров за выбранный период";
                    return;
                }

                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"Отчет_по_движению_товаров_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontFamily("Arial"));


                        page.Header().Column(column =>
                        {
                            column.Item().AlignCenter().Text("Отчет о движении товаров").FontSize(20).SemiBold();
                            column.Item().AlignCenter().Text($"за период с {_startDate:dd.MM.yyyy} по {_endDate:dd.MM.yyyy}").FontSize(14);
                        });
                       

                        // Таблица с данными
                        page.Content()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);  // Название товара
                                    columns.RelativeColumn(1);  // Приход
                                    columns.RelativeColumn(1);  // Продажа
                                    columns.RelativeColumn(1);  // Остаток
                                });

                                // Заголовки
                                table.Header(header =>
                                {
                                    header.Cell().Background("#EEE").Text("Товар").Bold();
                                    header.Cell().Background("#EEE").Text("Приход").Bold();
                                    header.Cell().Background("#EEE").Text("Продажа").Bold();
                                    header.Cell().Background("#EEE").Text("Остаток").Bold();
                                });

                                // Данные
                                foreach (var product in Products)
                                {
                                    var productMovements = filteredMovements
                                        .Where(m => m.ProductId == product.Id)
                                        .ToList();

                                    int received = productMovements
                                        .Where(m => m.MovementType == "incoming")
                                        .Sum(m => m.Quantity);

                                    int sold = productMovements
                                        .Where(m => m.MovementType == "sale")
                                        .Sum(m => m.Quantity);

                                    table.Cell().BorderBottom(1).Padding(5).Text(product.ProductName);
                                    table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(received.ToString("+0;-#"));
                                    table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(sold.ToString("-0;-#"));
                                    table.Cell().BorderBottom(1).Padding(5).AlignRight().Text((product.QuantityInStock ?? 0).ToString());
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x => x.CurrentPageNumber());
                    });
                })
                .GeneratePdf(filePath);

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                ErrorMessage = "Отчет успешно сформирован!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка: {ex.Message}";
                Debug.WriteLine(ex);
            }
        }


    }
}