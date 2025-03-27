using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GrillHouseNNProg.Models;
using GrillHouseNNProg.Resources;
using ReactiveUI;

namespace GrillHouseNNProg.ViewModels
{
    public class CreateOrderScreenViewModel : ViewModelBase
    {
        private readonly GrillcitynnContext _db;
        private Product _selectedProduct;
        private Discount _selectedDiscount;
        private int _enteredQuantity;
        private double _productPrice;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Discount> _discounts;
        private Dictionary<int, int> _soldStock; // Словарь для учета проданных товаров

        public CreateOrderScreenViewModel(GrillcitynnContext db)
        {
            _db = db;
            _soldStock = new Dictionary<int, int>(); // Инициализация словаря

            LoadProducts();
            LoadDiscounts();

            // Создание команды для оформления заказа
            CreateOrderCommand = ReactiveCommand.Create(CreateOrder);
        }

        /// <summary>
        /// Список товаров.
        /// </summary>
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => this.RaiseAndSetIfChanged(ref _products, value);
        }

        /// <summary>
        /// Список скидок.
        /// </summary>
        public ObservableCollection<Discount> Discounts
        {
            get => _discounts;
            set => this.RaiseAndSetIfChanged(ref _discounts, value);
        }

        /// <summary>
        /// Выбранный товар.
        /// </summary>
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedProduct, value);
                ProductPrice = _selectedProduct != null ? _selectedProduct.Price : 0; // Обновление цены при выборе товара
            }
        }

        /// <summary>
        /// Введенное количество товара.
        /// </summary>
        public int EnteredQuantity
        {
            get => _enteredQuantity;
            set => this.RaiseAndSetIfChanged(ref _enteredQuantity, value);
        }

        /// <summary>
        /// Цена выбранного товара.
        /// </summary>
        public double ProductPrice
        {
            get => _productPrice;
            set => this.RaiseAndSetIfChanged(ref _productPrice, value);
        }

        /// <summary>
        /// Выбранная скидка.
        /// </summary>
        public Discount SelectedDiscount
        {
            get => _selectedDiscount;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDiscount, value);
                ApplyDiscount(); // Применяем скидку при выборе
            }
        }

        /// <summary>
        /// Команда для оформления заказа.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CreateOrderCommand { get; }

        private void LoadProducts()
        {
            var productsFromDb = _db.Products.ToList();
            Products = new ObservableCollection<Product>(productsFromDb);
        }

        private void LoadDiscounts()
        {
            var disountFromDb = _db.Discounts.ToList();
            Discounts = new ObservableCollection<Discount>(disountFromDb);
        }

        private void ApplyDiscount()
        {
            if (SelectedDiscount != null && SelectedProduct != null)
            {
                ProductPrice = SelectedProduct.Price * (1 - (SelectedDiscount.DiscountPercent / 100.0));
            }
        }

        private void CreateOrder()
        {
            if (SelectedProduct != null && EnteredQuantity > 0)
            {
                var newOrder = new Order
                {
                    ProductId = SelectedProduct.Id,
                    DiscountId = SelectedDiscount?.Id,
                    DateOfOrder = DateOnly.FromDateTime(DateTime.Now),
                    FinalPrice = ProductPrice * EnteredQuantity
                };

                _db.Orders.Add(newOrder);

                var product = _db.Products.FirstOrDefault(p => p.Id == SelectedProduct.Id);
                if (product != null)
                {
                    product.QuantityInStock -= EnteredQuantity;
                    _db.Products.Update(product);
                    _db.SaveChanges();

                    // Запись в глобальный учет продаж
                    SoldStockTracker.RecordSale(product.Id, EnteredQuantity);
                }
            }
        }
    }
}