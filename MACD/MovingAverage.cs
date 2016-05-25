using System.Security.Cryptography.X509Certificates;

namespace MACD
{
    public interface IMovingAverage
    {
        void Add( double value );
        double GetAverage();
    }

    public class SimpleMovingAverage : IMovingAverage
    {
        private double[] data;

        private int index = 0;  // First available
        private int size = 0;

        private double average = 0;

        public SimpleMovingAverage( int length )
        {
            data = new double[length];
        }

        public void Add( double value )
        {
            if ( size < data.Length )
            {
                double sum = average * size;

                data[index] = value;
                index++;
                size++;

                sum += value;
                average = sum/size;
            }
            else
            {
                double sum = average * size;

                sum -= data[index];
                data[index] = value;
                sum += value;

                index++;

                average = sum / size;
            }

            if ( index >= data.Length )
                index = 0;
        }

        public double GetAverage()
        {
            return average;
        }
    }
}