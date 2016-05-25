using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MACD
{
    public class SimulationParams
    {
        public Stock Stock;
        public int DaysFast;
        public int DaysSlow;
        public int DaysAvg;
        public bool UseShorts;
        public double ShortCostPerDay = 1;
        public double FeePerShare = 0.01;
        public int DecisionLag;

        public int StartDay;
        public int TotalDays;

        public bool WriteLog = false;
        public bool VerboseLog = false;

        public double StartingMoney = 10000;
        public double MoneySwing;
        public double MoneyBuyHold;

        public bool LastOperationBuy;
        public bool NewOperationNow;
    }

    public class Analyzer
    {
        private List<Stock> stocks = new List<Stock>();
        private Log log = new Log();

        public Analyzer( List<string> fileNames )
        {
            foreach ( var fileName in fileNames )
            {
                var data = DataReader.ReadStockData( fileName );
                var stockName = fileName.Split( '\\' ).Last().Split( '.' ).First();
                data.Name = stockName;

                stocks.Add( data );
            }
        }

        public void DoIt()
        {
            int simDays = 180;
            int stepDays = 30;

            var results = new List<Tuple<double, double, string>>();

            for ( int f = 2; f <= 30; f++ )
            {
                for ( int s = f+1; s < 50; s++ )
                {
                    for ( int avg = 2; avg <= 10; avg++ )
                    {
                        for ( int lag = 0; lag < 1; lag++ ) // todo
                        {
                            int betterCount = 0;
                            int totalCount = 0;

                            foreach ( var stock in stocks )
                            {
                                var handles = new List<ManualResetEvent>();

                                for ( int day = 0; day < stock.PriceData.Count() - simDays; day += stepDays )
                                {
                                    var p = new SimulationParams
                                    {
                                        Stock = stock,
                                        DaysFast = f,
                                        DaysSlow = s,
                                        DaysAvg = avg,
                                        DecisionLag = lag,
                                        UseShorts = true,
                                        StartDay = day,
                                        TotalDays = simDays
                                    };

                                    var handle = new ManualResetEvent( false );
                                    handles.Add( handle );

                                    ThreadPool.QueueUserWorkItem( delegate
                                    {
                                        RunSimulation( p, log );

                                        lock ( this )
                                        {
                                            var profitBH = p.MoneyBuyHold - p.StartingMoney;
                                            var profitSwing = p.MoneySwing - p.StartingMoney;

                                            betterCount += profitSwing > profitBH ? 1 : 0;
                                            totalCount++;

                                            handle.Set();
                                        }
                                    } );
                                }

                                WaitHandle.WaitAll( handles.ToArray() );
                            }

                            results.Add( new Tuple<double, double, string>( betterCount, totalCount,
                                string.Format( "[f={0} s={1} avg={2} lag={3}]", f, s, avg, lag ) ) );
                        }
                    }
                }
            }

            // Set breakpoint after sort and observe top results
            results.Sort( ( t1, t2 ) => t2.Item1.CompareTo( t1.Item1 ) );

            // Test-run of one chosen parameter set
            foreach ( var stock in stocks )
            {
                var p = new SimulationParams
                {
                    Stock = stock,
                    DaysFast = 23,
                    DaysSlow = 37,
                    DaysAvg = 6,
                    //DaysFast = 23,
                    //DaysSlow = 29,
                    //DaysAvg = 10,
                    UseShorts = true,
                    StartDay = 0,
                    TotalDays = 99999,
                    WriteLog = true,
                    //VerboseLog = true
                };

                RunSimulation( p, log );
            }

            log.Dispose();
        }

        public static void RunSimulation( SimulationParams p, Log log )
        {
            int decisionScore = 0;

            double cash = p.StartingMoney;
            int shares = 0;

            int shorts = 0;
            double shortCash = 0;

            var fastMA = new SimpleMovingAverage( p.DaysFast );
            var slowMA = new SimpleMovingAverage( p.DaysSlow );
            var avgMA = new SimpleMovingAverage( p.DaysAvg );

            if ( p.WriteLog )
                log.AddLine( p.Stock.Name );

            double prevDvg = 0;
            int indexLast = Math.Min( p.StartDay + p.TotalDays, p.Stock.PriceData.Count ) - 1;
            for ( int index = p.StartDay; index <= indexLast; index++ )
            {
                var d = p.Stock.PriceData[index];

                fastMA.Add( d );
                slowMA.Add( d );

                var diff = fastMA.GetAverage() - slowMA.GetAverage();
                avgMA.Add( diff );

                var dvg = diff - avgMA.GetAverage();

                if ( p.WriteLog && p.VerboseLog )
                {
                    log.AddLine( string.Format(
                        "{6}: {0:0.0} f={1:0.00} s={2:0.00} diff={3:0.00} avg={4:0.00} dvg={5:0.00}",
                        d, fastMA.GetAverage(), slowMA.GetAverage(), diff, avgMA.GetAverage(), dvg, p.Stock.Dates[index] ) );
                }

                if ( shorts > 0 )
                    cash -= p.ShortCostPerDay;

                bool buy = false;
                bool sell = false;

                // Buy
                if ( dvg > 0 && prevDvg < 0 )
                {
                    buy = true;
                    if ( shorts > 0 )
                    {
                        if ( p.WriteLog && p.VerboseLog )
                            log.AddLine( string.Format( "--- Close short @ {0}: {1} for {2}---", d, shorts, d * shorts ) );

                        cash -= p.FeePerShare * shorts;
                        shortCash -= shorts * d;
                        shorts = 0;
                        cash += shortCash;
                        shortCash = 0;
                    }

                    shares = (int)( cash / d );
                    cash -= shares * d;
                    cash -= p.FeePerShare * shares;

                    if ( p.WriteLog && p.VerboseLog )
                    {
                        log.AddLine( string.Format( "--- Buy @ {0}: {1} for {2} ---", d, shares, d * shares ) );
                    }
                }

                // Sell
                if ( dvg < 0 && prevDvg > 0 )
                {
                    sell = true;
                    if ( p.WriteLog && p.VerboseLog )
                        log.AddLine( string.Format( "--- Sell @ {0}: {1} for {2} ---", d, shares, d * shares ) );

                    cash -= p.FeePerShare * shares;
                    cash += shares * d;
                    shares = 0;

                    if ( p.UseShorts )
                    {
                        shorts = (int)( cash / d );
                        shortCash = shorts * d;
                        cash -= p.FeePerShare * shorts;

                        if ( p.WriteLog && p.VerboseLog )
                            log.AddLine( string.Format( "--- Short @ {0}: {1} for {2} ---", d, shorts, d * shorts ) );
                    }
                }

                p.NewOperationNow = buy || sell;
                if ( p.NewOperationNow )
                    p.LastOperationBuy = buy;

                prevDvg = dvg;
            }

            shortCash -= shorts * p.Stock.PriceData[indexLast];
            cash += shortCash;
            cash += p.Stock.PriceData[indexLast] * shares;

            int bhAmount = (int)( p.StartingMoney / p.Stock.PriceData[p.StartDay] );
            double bhTotalCash = p.StartingMoney - p.Stock.PriceData[p.StartDay] * bhAmount + p.Stock.PriceData[indexLast] * bhAmount;

            double bhProfit = bhTotalCash - p.StartingMoney;
            double swingProfit = cash - p.StartingMoney;

            p.MoneyBuyHold = bhTotalCash;
            p.MoneySwing = cash;

            if ( p.WriteLog )
            {
                log.AddLine( string.Format( "Buy & Hold: {0} ({1:0.0}%)", bhProfit, ( bhProfit / p.StartingMoney ) * 100 ) );
                log.AddLine( string.Format( "Swing: {0} ({1:0.0}%)", swingProfit, ( swingProfit / p.StartingMoney ) * 100 ) );

                if ( p.VerboseLog )
                {
                    for ( int k = 0; k < 10; ++k ) 
                      log.AddLine( "" );
                }
            }
        }
    }
}