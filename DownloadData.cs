using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace StockAnalyzer
{
    class DownloadData
    {
        public List<string> GetArrayOfSymbols()
        {
            string filename = "Symbols.txt";
            string line;
            int counter = 0;
            List<string> symbols = new List<string>();
            StreamReader reader = new StreamReader(filename);
            while ((line = reader.ReadLine()) != null)
            {
                symbols.Add(line);
            }
            symbols.RemoveAt(0);
            foreach (string item in symbols)
            {
                Console.WriteLine(item);
            }
            return symbols;
        }
    }
}
