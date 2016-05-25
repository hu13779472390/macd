using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MACD;

namespace Advisor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window
    {
        private List<string> symbols = new List<string>();
        private List<Stock> stocks = new List<Stock>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnBtnDownloadClicked( object sender, RoutedEventArgs e )
        {
            symbols = tbSymbols.Text.Split( ' ' ).ToList();

            foreach ( var symbol in symbols )
            {
                var page = GetPage( string.Format( "http://real-chart.finance.yahoo.com/table.csv?s={0}&a=01&b=01&c=2016", symbol ), Encoding.ASCII );
                if ( string.IsNullOrWhiteSpace( page ) )
                {
                    MessageBox.Show( "Can't get Yahoo page for " + symbol );
                    continue;
                }

                var data = DataReader.ReadStockDataFromString( page );
                data.Name = symbol;

                stocks.Add( data );
            }

            MessageBox.Show( string.Format( "{0} histories downloaded", stocks.Count ) );
        }

        private void OnBtnParseGFClicked( object sender, RoutedEventArgs e )
        {
            var gfLines = tbGoogleFinance.Text.Split( '\n' );
            if ( gfLines.Length < 500 )
            {
                MessageBox.Show( "Something's wrong with Google Finance source" );
                return;
            }

            string symbol = string.Empty;
            double price;

            for ( int i = 500; i < Math.Min( 1000, gfLines.Length ); i++ )
            {
                var line = gfLines[i];

                if ( line.StartsWith( "<a href=\"/finance?q=" ) )
                {
                    var ind = line.IndexOf( '>' );
                    if ( ind != -1 )
                        symbol = line.Substring( ind + 1 ).TrimEnd( '\r' );
                }

                if ( line.StartsWith( "<td class=price>" ) )
                {
                    var ind = line.IndexOf( '>', 20 );
                    if ( ind == -1 )
                        continue;

                    var tmp = line.Substring( ind + 1 );
                    ind = tmp.IndexOf( '<' );
                    if ( ind == -1 )
                        continue;
                    tmp = tmp.Substring( 0, ind );
                    price = double.Parse( tmp, CultureInfo.InvariantCulture );

                    AddTodayData( symbol, price );
                    symbols.Remove( symbol );
                }
            }

            if ( symbols.Count > 0 )
            {
                MessageBox.Show( "Current price not found for symbols: " +
                                 symbols.Aggregate( ( current, next ) => current + ", " + next ) );
            }
            else
            {
                MessageBox.Show( "All prices parsed successfully" );
            }
        }

        private void OnBtnAnalyzeClicked( object sender, RoutedEventArgs e )
        {
            var settings = tbSettings.Text.Split( ' ' );
            if ( settings.Length != 3 )
            {
                MessageBox.Show( "Expected exactly 3 parameters, got " + settings.Length );
                return;
            }

            foreach ( var stock in stocks )
            {
                var p = new SimulationParams
                {
                    Stock = stock,
                    DaysFast = int.Parse( settings[0] ),
                    DaysSlow = int.Parse( settings[1] ),
                    DaysAvg = int.Parse( settings[2] ),
                    UseShorts = true,
                    StartDay = 0,
                    TotalDays = 999999,
                    WriteLog = true,
                    VerboseLog = true
                };

                var log = new Log( stock.Name );
                Analyzer.RunSimulation( p, log );
                OutputResult( p );
                log.Dispose();
            }
        }

        private void OutputResult( SimulationParams p )
        {
            spResults.Children.Add( new ResultControl( p ) );
        }

        private void AddTodayData( string symbol, double price )
        {
            var s = stocks.FirstOrDefault( st => st.Name == symbol );
            if ( s == null )
                return;

            s.PriceData.Add( price );
            s.Dates.Add( "Today" );
        }

        public static string GetPage( string url, Encoding encoding )
        {
            try
            {
                var request = WebRequest.Create( url ) as HttpWebRequest;
                request.Method = WebRequestMethods.Http.Get;
                request.Headers.Add( "Accept-Language", "en-US" );
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Win32)";

                Stream responseStream = request.GetResponse().GetResponseStream();

                if ( responseStream == null )
                    return string.Empty;
                StreamReader reader = new StreamReader( responseStream, encoding );

                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }
    }
}
