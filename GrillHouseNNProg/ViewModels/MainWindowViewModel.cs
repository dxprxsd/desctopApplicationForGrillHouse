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

        private Provider _selectedSupplier;
        private List<Provider> _suppliers;
        private List<Provider> _allSuppliers;

        private bool _isProductListEmpty;
        private bool _typesLoaded; // Флаг для отслеживания загрузки типов продуктов
        private List<Product> _allProducts; // Полный список всех товаров

        private bool _sortByPriceAscending = true; //для фильтрации по возрастанию/убыванию цены
        private string _searchQuery; // для поиска по названию товара
        public string SortButtonText => SortByPriceAscending ? "Сортировка: ↑ Цена" : "Сортировка: ↓ Цена";

        public bool SortByPriceAscending
        {
            get => _sortByPriceAscending;
            set
            {
                this.RaiseAndSetIfChanged(ref _sortByPriceAscending, value);
                this.RaisePropertyChanged(nameof(SortButtonText));
                FilterProducts();
            }

        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchQuery, value);
                FilterProducts(); // Фильтруем при изменении поискового запроса
            }
        }

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
        public List<Provider> AllSuppliers
        {
            get => _allSuppliers;
            set => this.RaiseAndSetIfChanged(ref _allSuppliers, value);
        }

        public Provider SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSupplier, value);
                FilterProducts();
            }
        }

        public List<Provider> Suppliers
        {
            get => _suppliers;
            set => this.RaiseAndSetIfChanged(ref _suppliers, value);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => this.RaiseAndSetIfChanged(ref _products, value);
        }

        public ReactiveCommand<Unit, Unit> ResetFiltersCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleSortByPriceCommand { get; }

        public MainWindowViewModel()
        {
            Self = this;
            Us = new MainScreen();
            LoadDataAsync(); // Изменил имя метода на LoadDataAsync для консистентности
            _ = LoadTypes();
            ResetFiltersCommand = ReactiveCommand.Create(ResetFilters);
            ToggleSortByPriceCommand = ReactiveCommand.Create(() =>
            {
                SortByPriceAscending = !SortByPriceAscending;
            });

        }

        private async Task LoadTypes()
        {
            try
            {
                ProductTypess = await _apiService.GetProductTypesAsync();

                AllSuppliers = await _apiService.GetProvidersAsync();
                Console.WriteLine($"Поставщиков загружено: {AllSuppliers.Count}");

                foreach (var s in AllSuppliers)
                    Console.WriteLine($"Поставщик: {s.ProviderName} (ID: {s.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке типов/поставщиков: {ex.Message}");
            }
        }



        async void LoadDataAsync()
        {
            try
            {
                // Загружаем данные
                ProductTypess = await _apiService.GetProductTypesAsync();
                AllSuppliers = await _apiService.GetProvidersAsync();
                _allProducts = await _apiService.GetProductsAsync();

                Products = new ObservableCollection<Product>(_allProducts);

                // Фильтрация запускается без установленных фильтров
                FilterProducts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            }
        }


        private void FilterProducts()
        {
            if (_allProducts == null) return;

            IEnumerable<Product> filtered = _allProducts;

            if (SelectedProductType != null)
                filtered = filtered.Where(p => p.ProductTypeId == SelectedProductType.Id);

            if (SelectedSupplier != null)
                filtered = filtered.Where(p => p.ProviderId == SelectedSupplier.Id);

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var query = SearchQuery.Trim().ToLower();
                filtered = filtered.Where(p =>
                    p.ProductName.ToLower().Contains(query) ||
                    (p.Provider?.ProviderName != null && p.Provider.ProviderName.ToLower().Contains(query)));
            }

            // Сортировка по цене
            filtered = SortByPriceAscending
                ? filtered.OrderBy(p => p.Price)
                : filtered.OrderByDescending(p => p.Price);

            Products = new ObservableCollection<Product>(filtered);
            IsProductListEmpty = !Products.Any();

            Console.WriteLine($"Отфильтровано {Products.Count} товаров, сортировка: {(SortByPriceAscending ? "↑" : "↓")}");
        }



        public void ResetFilters()
        {
            SelectedProductType = null;
            SelectedSupplier = null;
            SearchQuery = string.Empty;

            Products = new ObservableCollection<Product>(_allProducts);
            IsProductListEmpty = !Products.Any();

            Console.WriteLine("Все фильтры сброшены");
        }


        public void GoToMainScreen() => Us = new MainScreen();
        public void GoToCreateOrderScreen() => Us = new CreateOrderScreen() { DataContext = new CreateOrderScreenViewModel(_apiService) };
        public void GoToOrderHistoryScreen() => Us = new OrderHistoryScreen() { DataContext = new OrderHistoryScreenViewModel(_apiService) };
        public void GoToAddinCountProductScreen() => Us = new AddingCountProductsScreen() { DataContext = new AddingCountProductsScreenViewModel(_apiService) };
    }


}
