using GrillHouseNNProg.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Diagnostics;
using NewtonsoftJson = Newtonsoft.Json;
using SystemJson = System.Text.Json;

namespace GrillHouseNNProg.Resources
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5178");
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Устанавливаем таймаут
        }

        public async Task<List<Product>> GetProductsAsync(int typeId = 0)
        {
            var response = await _httpClient.GetAsync($"/products?typeId={typeId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonConvert.DeserializeObject<List<Product>>(json);
            return products;
        }

        // New method for /productss endpoint
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var response = await _httpClient.GetAsync("/productss");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Product>>();
        }

        

        public async Task<List<ProductType>> GetProductTypesAsync()
        {
            var response = await _httpClient.GetAsync("/productTypes");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ProductType>>(json);
        }

        // Updated method for /discounts endpoint
        public async Task<List<Discount>> GetDiscountsAsync()
        {
            var response = await _httpClient.GetAsync("/discounts");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Discount>>();
        }

        // New method for /CreateOrder endpoint
        public async Task<OrderResult> CreateOrderAsync(int productId, int? discountId, int quantity)
        {
            try
            {
                // Формируем URL с параметрами в query string
                var url = $"/CreateOrder?productId={productId}&quantity={quantity}";

                // Добавляем discountId, если он указан
                if (discountId.HasValue)
                {
                    url += $"&discountId={discountId.Value}";
                }

                // Отправляем POST запрос с пустым телом
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Ошибка API: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<OrderResult>(responseContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в CreateOrderAsync: {ex}");
                throw;
            }
        }

        public async Task CreateOrderAsync(Order order)
        {
            var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/orders", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var response = await _httpClient.GetAsync("/orders");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Order>>(json);
        }

        public async Task<List<Order>> GetOrdersByDateAsync(DateTime start, DateTime end)
        {
            var response = await _httpClient.GetAsync($"/ordersByDate?startDate={start:O}&endDate={end:O}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Order>>(json);
        }

        // Метод для обновления количества товара
        public async Task<string> UpdateProductStockAsync(int productId, int quantity)
        {
            try
            {
                // Вариант 1: Отправка параметров в query string (если сервер требует)
                var url = $"/updateProductStock?productId={productId}&quantity={quantity}";
                var response = await _httpClient.PostAsync(url, null);

                // Вариант 2: Отправка JSON в теле (рекомендуется, если можно изменить сервер)
                /*
                var requestData = new { productId, quantity };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/updateProductStock", content);
                */

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Ошибка сервера: {response.StatusCode} - {errorContent}");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в UpdateProductStockAsync: {ex}");
                throw;
            }
        }

        public async Task<List<ProductMovementDto>> GetProductMovementsAsync(DateTime startDate, DateTime endDate)
        {
            var response = await _httpClient.GetAsync("/getProductMovements");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ProductMovementDto>>(json);
        }

        public async Task<List<Provider>> GetProvidersAsync()
        {
            var response = await _httpClient.GetAsync("/providers");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Provider>>();
        }
    }

    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; }  // Соответствует ProviderName в модели Provider

        // Можно добавить другие свойства, если они будут в API
        // public string ContactInfo { get; set; }
        // public string Address { get; set; }
    }

    // DTO класс для движения товара
    public class ProductMovementDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string MovementType { get; set; }
        public DateTime MovementDate { get; set; }  // Изменили на DateTime для удобства
    }


    // Class to represent the order result
    public class OrderResult
    {
        public string Message { get; set; }
        public int OrderId { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public double FinalPrice { get; set; }
    }
}
