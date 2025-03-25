using Avalonia.Controls;
using GrillHouseNNProg.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.VisualBasic;

namespace GrillHouseNNProg.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly GrillcitynnContext _db;
        private UserControl _us;
        public static MainWindowViewModel Self;

        private Product _productt;
        private ObservableCollection<Product> _productss;

        private ProductType _selectedProductType;
        private List<ProductType> _productTypes;

        private bool _isProductListEmpty;
        public bool IsProductListEmpty
        {
            get => _isProductListEmpty;
            set => this.RaiseAndSetIfChanged(ref _isProductListEmpty, value);
        }


        /// <summary>
        /// Контроллер текущего экрана.
        /// </summary>
        public UserControl Us
        {
            get => _us;
            set => this.RaiseAndSetIfChanged(ref _us, value);
        }

        /// <summary>
        /// Выбранный товар.
        /// </summary>
        public Product Productt
        {
            get => _productt;
            set => this.RaiseAndSetIfChanged(ref _productt, value);
        }

        /// <summary>
        /// Список доступных полов для стрижек.
        /// </summary>
        public List<ProductType> ProductTypes
        {
            get => _productTypes;
            set => this.RaiseAndSetIfChanged(ref _productTypes, value);
        }

        /// <summary>
        /// Выбранный пол для фильтрации стрижек.
        /// </summary>
        public ProductType SelectedProductType
        {
            get => _selectedProductType;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedProductType, value);
                FilterHaircuts();
            }
        }

        /// <summary>
        /// Список товаров.
        /// </summary>
        public ObservableCollection<Product> Productss
        {
            get => _productss;
            set => this.RaiseAndSetIfChanged(ref _productss, value);
        }

        public MainWindowViewModel(GrillcitynnContext db)
        {
            _db = db;
            Self = this;
            Us = new MainScreen();
            LoadProducts();
        }

        void LoadProducts()
        {
            // Добавляем "Все виды" в список полов
            var allTypes = new ProductType { Id = 0, TypeName = "Все виды" };
            ProductTypes = new List<ProductType> { allTypes };
            ProductTypes.AddRange(_db.ProductTypes.ToList());

            Productss = new ObservableCollection<Product>(_db.Products.Include(x => x.ProductType).ToList());

            // По умолчанию выбираем "Все виды"
            SelectedProductType = allTypes;

        }


        /// <summary>
        /// Фильтрует стрижки по выбранному полу.
        /// </summary>
        private void FilterHaircuts()
        {
            if (SelectedProductType != null && SelectedProductType.Id != 0)
            {
                Productss = new ObservableCollection<Product>(_db.Products
                    .Include(x => x.ProductType)
                    .Where(x => x.ProductTypeId == SelectedProductType.Id)
                    .ToList());
            }
            else
            {
                Productss = new ObservableCollection<Product>(_db.Products.Include(x => x.ProductType).ToList());
            }

            // Проверяем, пуст ли список товаров
            IsProductListEmpty = Productss.Count == 0;
        }
    }
}
