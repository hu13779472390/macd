using System;
using System.IO;
using System.Text;

namespace MACD
{
    public class Log: IDisposable
    {
        private string fileName;
        private StringBuilder sb = new StringBuilder();

        public Log( string fileName = "" )
        {
            this.fileName = fileName;
        }

        public void AddLine( string str )
        {
            sb.AppendLine( str );
        }

        public void Dispose()
        {
            File.WriteAllText( string.IsNullOrWhiteSpace( fileName ) ? "Log.txt" : fileName + ".txt", sb.ToString() );
        }
    }
}