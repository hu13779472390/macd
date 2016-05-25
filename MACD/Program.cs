using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MACD
{
    class Program
    {
        static void Main( string[] args )
        {
            var dataFolder = "Data-2.5-years";

            var files = Directory.EnumerateFiles( dataFolder ).ToList();

            var analyzer = new Analyzer( files );
            analyzer.DoIt();

            var sma = new SimpleMovingAverage( 3 );
            sma.Add( 1 );
            var avg = sma.GetAverage();
            sma.Add( 2 );
            avg = sma.GetAverage();
            sma.Add( 3 );
            avg = sma.GetAverage();
            sma.Add( 1 );
            avg = sma.GetAverage();
            sma.Add( -1 );
            avg = sma.GetAverage();
        }
    }
}
