using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrillHouseNNProg.Resources
{
    public static class SoldStockTracker
    {
        private static Dictionary<int, int> _soldStock = new Dictionary<int, int>();

        public static void RecordSale(int productId, int quantity)
        {
            if (_soldStock.ContainsKey(productId))
                _soldStock[productId] += quantity;
            else
                _soldStock[productId] = quantity;
        }

        public static int GetSoldQuantity(int productId)
        {
            return _soldStock.TryGetValue(productId, out var quantity) ? quantity : 0;
        }

        public static Dictionary<int, int> GetAllSoldStock()
        {
            return new Dictionary<int, int>(_soldStock);
        }
    }

}
