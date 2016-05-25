using System.Collections.Generic;

namespace MACD
{
    public class Stock
    {
        public string Name;

        public List<double> PriceData = new List<double>();
        public List<string> Dates = new List<string>();
    }
}