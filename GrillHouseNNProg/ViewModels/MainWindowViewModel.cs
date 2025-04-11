using Avalonia.Controls;
using GrillHouseNNProg.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using GrillHouseNNProg.Views;
using GrillHouseNNProg.Resources;
using System;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Threading.Tasks;
using System.Reactive;

namespace GrillHouseNNProg.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        ApiService _apiService = new ApiService();
        private readonly GrillcitynnContext _db;
        private UserControl _us;
        public static MainWindowViewModel Self;

        private Product _productt;
        private ObservableCollection<Product> _products;

        ProductType _selectedProductType;
        private List<ProductType> _productTypes;
        private List<ProductType> _productTypess;


        private bool _isProductListEmpty;
        private bool _typesLoaded; // Флаг для отслеживания загрузки типов продуктов
        private List<Product> _allProducts; // Полный список всех товаров

        public bool IsProductListEmpty
        {
            get => _isProductListEmpty;
            set => this.RaiseAndSetIfChanged(ref _isProductListEmpty, value);
        }

        public UserControl Us
        {
            get => _us;
            set => this.RaiseAndSetIfChanged(ref _us, value);
        }

        public Product Productt
        {
            get => _productt;
            set => this.RaiseAndSetIfChanged(ref _productt, value);
        }

        public List<ProductType> ProductTypes
        {
            get => _productTypes;
            set => this.RaiseAndSetIfChanged(ref _productTypes, value);
        }

        public List<ProductType> ProductTypess
        {
            get => _productTypess;
            set => this.RaiseAndSetIfChanged(ref _productTypess, value);
        }

        public ProductType SelectedProductType
        {
            get => _selectedProductType;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedProductType, value);
                FilterProducts(); // Вызываем фильтрацию при изменении выбранного типа
            }
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => this.RaiseAndSetIfChanged(ref _products, value);
        }

        public ReactiveCommand<Unit, Unit> ResetFiltersCommand { get; }

        public MainWindowViewModel()
        {
            Self = this;
            Us = new MainScreen();
            LoadDataAsync(); // Изменил имя метода на LoadDataAsync для консистентности
            _ = LoadTypes();
            ResetFiltersCommand = ReactiveCommand.Create(ResetFilters);
        }

        private async Task LoadTypes()
        {
            try
            {
                // Загрузка гендеров из базы данных или API (должно быть реализовано в ApiService)
                ProductTypess = await _apiService.GetProductTypesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load genders: {ex.Message}");
            }
        }

        async void LoadDataAsync()
        {
            try
            {
                // Загружаем типы продуктов из API
                var typesFromApi = await _apiService.GetProductTypesAsync();

                // Создаем элемент "Все типы"
                var allTypes = new ProductType { Id = 0, TypeName = "Все типы" };

                //// Формируем полный список типов
                //ProductTypes = new List<ProductType> { allTypes };
                //ProductTypes.AddRange(typesFromApi); // Добавляем остальные типы

                // Загружаем все товары из API
                _allProducts = await _apiService.GetProductsAsync();

                // Инициализируем список товаров
                Products = new ObservableCollection<Product>(_allProducts);

                // Устанавливаем "Все типы" по умолчанию
                SelectedProductType = allTypes;

                // Включаем фильтрацию сразу после загрузки данных
                FilterProducts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            }
        }

        private void FilterProducts()
        {
            if (SelectedProductType == null || _allProducts == null) return;

            // Фильтруем продукты по типу
            var filtered = SelectedProductType.Id == 0
                ? _allProducts // "Все типы" - показываем все товары
                : _allProducts.Where(p => p.ProductTypeId == SelectedProductType.Id).ToList();

            // Обновляем список продуктов
            Products = new ObservableCollection<Product>(filtered);
            IsProductListEmpty = Products.Count == 0; // Проверяем, пуст ли список товаров

            Console.WriteLine($"Filtered {Products.Count} products for type {SelectedProductType.Id}");
        }

        public void ResetFilters()
        {
            // Устанавливаем "Все типы" и сбрасываем фильтрацию
            SelectedProductType = ProductTypess.FirstOrDefault(type => type.Id == 0); // "Все типы"

            // Теперь фильтруем продукты с учетом сброса фильтра
            Products = new ObservableCollection<Product>(_allProducts);

            // Обновляем статус пустого списка
            IsProductListEmpty = Products.Count == 0;

            Console.WriteLine($"Filtered {Products.Count} products (reset filters)");
        }



        public void GoToMainScreen() => Us = new MainScreen();
        public void GoToCreateOrderScreen() => Us = new CreateOrderScreen() { DataContext = new CreateOrderScreenViewModel(_apiService) };
        public void GoToOrderHistoryScreen() => Us = new OrderHistoryScreen() { DataContext = new OrderHistoryScreenViewModel(_apiService) };
        public void GoToAddinCountProductScreen() => Us = new AddingCountProductsScreen() { DataContext = new AddingCountProductsScreenViewModel(_apiService) };
    }


}
