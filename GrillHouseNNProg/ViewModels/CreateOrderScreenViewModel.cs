using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using GrillHouseNNProg.Models;
using GrillHouseNNProg.Resources;
using ReactiveUI;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Reactive;
using System.Diagnostics;

namespace GrillHouseNNProg.ViewModels
{
    public class CreateOrderScreenViewModel : ViewModelBase
    {
        private readonly ApiService _apiService;
        private Product _selectedProduct;
        private Discount _selectedDiscount;
        private int? _enteredQuantity;
        private double _productPrice;
        private ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private ObservableCollection<Discount> _discounts = new ObservableCollection<Discount>();
        private string _errorMessage;

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public CreateOrderScreenViewModel(ApiService apiService)
        {

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _isLoading = false; // Инициализация


            // Инициализация коллекций перед загрузкой
            Products = new ObservableCollection<Product>();
            Discounts = new ObservableCollection<Discount>();

            // Асинхронная загрузка данных
            _ = LoadDataAsync();

            CreateOrderCommand = ReactiveCommand.CreateFromTask(CreateOrderAsync);
        }

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

        public ObservableCollection<Discount> Discounts
        {
            get => _discounts;
            set => this.RaiseAndSetIfChanged(ref _discounts, value);
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedProduct, value);
                ProductPrice = _selectedProduct?.Price ?? 0;
                UpdateProductPrice();
            }
        }

        public int? EnteredQuantity
        {
            get => _enteredQuantity;
            set
            {
                this.RaiseAndSetIfChanged(ref _enteredQuantity, value);
                UpdateProductPrice();
            }
        }

        public double ProductPrice
        {
            get => _productPrice;
            set => this.RaiseAndSetIfChanged(ref _productPrice, value);
        }

        public Discount SelectedDiscount
        {
            get => _selectedDiscount;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDiscount, value);
                UpdateProductPrice();
            }
        }

        public ReactiveCommand<Unit, Unit> CreateOrderCommand { get; }

        public async Task LoadDataAsync()
        {
            try
            {
                // Загрузка продуктов
                var products = await _apiService.GetAllProductsAsync();
                Products = new ObservableCollection<Product>(products);

                // Загрузка скидок
                var discounts = await _apiService.GetDiscountsAsync();
                Discounts = new ObservableCollection<Discount>(discounts);

                ErrorMessage = ""; // Очищаем сообщение об ошибке при успешной загрузке
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
                // Для отладки можно добавить логирование
                Debug.WriteLine($"Ошибка при загрузке данных: {ex}");
            }
        }

        public void UpdateProductPrice()
        {
            if (SelectedProduct != null && EnteredQuantity.HasValue && EnteredQuantity.Value > 0)
            {
                double basePrice = SelectedDiscount != null
                    ? SelectedProduct.Price * (1 - (SelectedDiscount.DiscountPercent / 100.0))
                    : SelectedProduct.Price;

                ProductPrice = basePrice * EnteredQuantity.Value;
            }
            else
            {
                ProductPrice = 0;
            }
        }

        private async Task CreateOrderAsync()
        {
            try
            {
                // Проверка данных
                if (SelectedProduct == null)
                {
                    ErrorMessage = "Не выбран продукт";
                    return;
                }

                if (!EnteredQuantity.HasValue || EnteredQuantity.Value <= 0)
                {
                    ErrorMessage = "Укажите корректное количество";
                    return;
                }

                IsLoading = true;
                ErrorMessage = string.Empty;

                // Логирование перед отправкой
                Debug.WriteLine($"Создание продажи: ProductId={SelectedProduct.Id}, " +
                              $"DiscountId={SelectedDiscount?.Id}, Quantity={EnteredQuantity.Value}");

                var result = await _apiService.CreateOrderAsync(
                    SelectedProduct.Id,
                    SelectedDiscount?.Id,
                    EnteredQuantity.Value
                );

                ErrorMessage = "Продажа прошла успешно!";
                SaveOrderToExcel(result);
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("400"))
            {
                ErrorMessage = "Ошибка в данных заказа. Проверьте введенные значения.";
                Debug.WriteLine(httpEx);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка: {ex.Message}";
                Debug.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void SaveOrderToExcel(OrderResult orderResult)
        {
            string orderFileName = $"Order_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string orderFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, orderFileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Заказ");

                // Заполнение Excel-файла (остается без изменений)
                worksheet.Cell("A1").Value = "ИП Узлов Ю. В. ИНН 526312046689";
                worksheet.Range("A1:E1").Merge().Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                worksheet.Cell("A2").Value = "(наименование организации, ИНН)";
                worksheet.Range("A2:F2").Merge().Style.Font.FontSize = 6;
                worksheet.Cell("A3").Value = "Товарный чек № ________ от ________________ г.";
                worksheet.Range("A3:F3").Merge().Style.Font.FontSize = 12;
                worksheet.Range("A3:F3").Style.Font.Bold = true;

                worksheet.Cell("A5").Value = "Наименование товара";
                worksheet.Cell("B5").Value = "Единица измерения";
                worksheet.Cell("C5").Value = "Количество";
                worksheet.Cell("D5").Value = "Цена за штуку";
                worksheet.Cell("E5").Value = "Сумма";

                worksheet.Cell("A6").Value = orderResult.Product;
                worksheet.Cell("B6").Value = "шт.";
                worksheet.Cell("C6").Value = orderResult.Quantity;

                worksheet.Cell("D6").Value = SelectedProduct.Price;
                worksheet.Cell("D6").Style.NumberFormat.Format = "0.00";

                worksheet.Cell("E6").Value = orderResult.FinalPrice;
                worksheet.Cell("E6").Style.NumberFormat.Format = "0.00";

                worksheet.Range("A5:E6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Columns().AdjustToContents();

                if (SelectedDiscount != null)
                {
                    worksheet.Cell("A7").Value = $"Скидка: {SelectedDiscount.DiscountPercent}%";
                    worksheet.Range("A7:E7").Merge().Style.Font.FontSize = 11;
                }

                string totalAmountWords = ConvertToWords(orderResult.FinalPrice);

                worksheet.Cell("A8").Value = "Всего отпущено на сумму:";
                worksheet.Cell("A9").Value = totalAmountWords;
                worksheet.Range("A8").Merge().Style.Font.FontSize = 11;

                worksheet.Cell("A11").Value = "Продавец";
                worksheet.Cell("B11").Value = "_________";
                worksheet.Cell("C11").Value = "_________";
                worksheet.Cell("B12").Value = "подпись";
                worksheet.Cell("C12").Value = "ФИО";

                workbook.SaveAs(orderFilePath);
            }
        }


        public string ConvertToWords(double number)
        {
            // Массивы для единиц, десятков и сотен
            string[] ones = new string[] { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять", "десять",
        "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
            string[] tens = new string[] { "", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            string[] hundreds = new string[] { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
            string[] thousands = new string[] { "", "тысяча", "тысячи", "тысяч" };

            if (number == 0)
                return "Ноль руб. 00 коп.";

            int rubles = (int)number;
            int kopecks = (int)Math.Round((number - rubles) * 100);

            string words = "";

            // Обработка тысяч
            if (rubles >= 1000)
            {
                int thousandPart = rubles / 1000;
                rubles %= 1000;

                // Сотни тысяч
                if (thousandPart >= 100)
                {
                    int hundredThousand = thousandPart / 100;
                    words += hundreds[hundredThousand] + " ";
                    thousandPart %= 100;
                }

                // Десятки тысяч
                if (thousandPart >= 20)
                {
                    int tenThousand = thousandPart / 10;
                    words += tens[tenThousand] + " ";
                    thousandPart %= 10;
                }

                // Единицы тысяч (особые формы)
                if (thousandPart > 0)
                {
                    if (thousandPart == 1)
                        words += "одна ";
                    else if (thousandPart == 2)
                        words += "две ";
                    else if (thousandPart < 20)
                        words += ones[thousandPart] + " ";
                }

                // Добавляем правильную форму слова "тысяча"
                int lastThousandDigit = (thousandPart % 10);
                if (thousandPart >= 11 && thousandPart <= 19)
                    words += thousands[3] + " "; // тысяч
                else if (lastThousandDigit == 1)
                    words += thousands[1] + " "; // тысяча
                else if (lastThousandDigit >= 2 && lastThousandDigit <= 4)
                    words += thousands[2] + " "; // тысячи
                else
                    words += thousands[3] + " "; // тысяч
            }

            // Обработка сотен рублей
            if (rubles >= 100)
            {
                int hundred = rubles / 100;
                words += hundreds[hundred] + " ";
                rubles %= 100;
            }

            // Обработка десятков рублей
            if (rubles >= 20)
            {
                int ten = rubles / 10;
                words += tens[ten] + " ";
                rubles %= 10;
            }

            // Обработка единиц рублей
            if (rubles > 0)
            {
                words += ones[rubles] + " ";
            }

            // Добавляем "руб." и количество копеек
            words = words.Trim() + " руб. ";

            // Обработка копеек
            words += kopecks.ToString("D2") + " коп.";

            // Делаем первую букву заглавной
            if (words.Length > 0)
            {
                words = char.ToUpper(words[0]) + words.Substring(1);
            }

            return words;
        }

        // Метод для обработки тысяч
        public string HandleThousands(int part, string[] largeNumbers)
        {
            int index = part % 10;
            int tensPart = (part / 10) % 10;

            string result = "";

            // Для чисел от 11 до 14 (исключения для склонений)
            if (tensPart == 1)
            {
                result = largeNumbers[3]; // "тысяч"
            }
            else if (index == 1)
            {
                result = largeNumbers[1]; // "тысяча"
            }
            else if (index >= 2 && index <= 4)
            {
                result = largeNumbers[2]; // "тысячи"
            }
            else
            {
                result = largeNumbers[3]; // "тысяч"
            }

            return result;
        }


    }
}
