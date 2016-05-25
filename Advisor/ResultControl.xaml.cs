using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for ResultControl.xaml
    /// </summary>
    public partial class ResultControl: UserControl
    {
        public ResultControl( SimulationParams sp )
        {
            InitializeComponent();

            tbSymbol.Text = sp.Stock.Name;
            tbPrice.Text = sp.Stock.PriceData.Last().ToString();

            tbOperation.Text = sp.LastOperationBuy ? "Buy" : "Sell";

            if ( sp.NewOperationNow )
            {
                rectOperation.Fill = sp.LastOperationBuy ? Brushes.DarkGreen : Brushes.Red;
                tbOperation.Foreground = Brushes.White;

                tbNoChange.Visibility = Visibility.Hidden;
            }
            else
            {
                tbOperation.Foreground = sp.LastOperationBuy ? Brushes.DarkGreen : Brushes.Red;
                rectOperation.Fill = Brushes.Transparent;

                tbNoChange.Visibility = Visibility.Visible;
            }
        }
    }
}
