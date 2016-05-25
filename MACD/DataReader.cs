using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MACD
{
    public class DataReader
    {
        public static Stock ReadStockData( string fileName )
        {
            var result = new Stock();

            var lines = File.ReadAllLines( fileName );
            Load( lines, result );

            return result;
        }

        public static Stock ReadStockDataFromString( string str )
        {
            var result = new Stock();

            var lines = str.Split( '\n' );
            Load( lines, result );

            return result;
        }

        private static void Load( string[] lines, Stock result )
        {
            foreach ( var line in lines.Skip( 1 ) )
            {
                var parts = line.Split( ',' );
                if ( parts.Length != 7 )
                    continue;

                double price = double.Parse( parts[4], CultureInfo.InvariantCulture );
                result.PriceData.Add( price );
                result.Dates.Add( parts[0] );
            }

            result.Dates.Reverse();
            result.PriceData.Reverse();
        }
    }
}