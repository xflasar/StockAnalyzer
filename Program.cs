using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace StockAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {

            int success = 0;
            int failed = 0;
            string str = "";
            List<string> SymbolList = new List<string>();
            List<string> StocksDB = new List<string>();
            SaveDataToDatabase("", SymbolList);
            while (true)
            {
                DownloadData dnwData = new DownloadData();
                SymbolList = dnwData.GetArrayOfSymbols();
                if (SymbolList.Count <= 0)
                {
                    int pageNum = 1;
                    while (true)
                    {
                        try
                        {
                            var watch1 = System.Diagnostics.Stopwatch.StartNew();

                            str = Program.GetDataFromServer(@"https://www.nasdaq.com/api/v1/screener?page=" + pageNum + "&pageSize=20");
                            // the code that you want to measure comes here
                            watch1.Stop();
                            var elapsedMs1 = watch1.ElapsedMilliseconds;
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("404"))
                            {
                                return;
                            }
                            str = "";
                        }

                        if (str != "" && str != "{\"data\":[],\"count\":6055}")
                        {

                            List<string> strr = new List<string>();
                            string[] strArray = Regex.Split(str, "\"ticker\":");
                            foreach (string item in strArray)
                            {
                                strr.Add(item);

                            }
                            strr.RemoveAt(0);
                            foreach (string item in strr)
                            {

                                try
                                {
                                    string strrr = item.Substring(0, item.IndexOf(","));
                                    List<char> tempStr = strrr.ToList();
                                    tempStr.RemoveAt(0);
                                    tempStr.RemoveAt(tempStr.Count - 1);

                                    strrr = String.Join("", tempStr.ToArray());
                                    //Console.WriteLine(strrr);
                                    SymbolList.Add(strrr);
                                }
                                catch (Exception ex)
                                {

                                    //Console.WriteLine(ex + " thrown!");
                                }

                            }
                        }
                        else
                        {
                            break;
                        }
                        pageNum++;
                        Console.WriteLine(pageNum);
                    }
                }
                else
                {
                    //Program.SaveSymbolsToFile(SymbolList);

                    foreach (string symbol in SymbolList)
                    {
                        str = Program.GetDataFromServer(@"https://query1.finance.yahoo.com/v8/finance/chart/" + symbol);
                        if (str != "")
                        {

                            if (SaveDataToDatabase(str, SymbolList))
                            {
                                success++;
                            }
                            else
                            {
                                failed++;
                            }
                        }
                        Console.Clear();
                        Console.WriteLine("Succeed = " + success);
                        Console.WriteLine("Failed = " + failed);
                    }
                    Console.ReadKey();
                    return;
                }
            }



            /*
            DownloadData dwnData = new DownloadData();
            dwnData.GetArrayOfSymbols();*/
            Console.ReadKey();
        }

        public static void SaveSymbolsToFile(List<string> SymbolsList)
        {
            using (var filename = new FileStream("Symbols.txt",FileMode.Append))
            using (var writer = new StreamWriter(filename))
            {

                foreach (string item in SymbolsList)
                {
                    writer.WriteLine(item);
                }
            }
        }


        private static List<string> ParseData(string data, List<string> Symbols)
        {
            List<string> dataTempList = new List<string>();
            try
            {
                //  Symbol Parser
                int symbolStartPos = data.IndexOf("symbol\":\"") + 9;
                string symbolStrTemp = data.Substring(symbolStartPos, data.IndexOf("\"", symbolStartPos) - symbolStartPos);

                //  Test
                if (Symbols.Contains(symbolStrTemp))
                {
                    dataTempList.Add(symbolStrTemp);
                }
                else
                {
                    return new List<string>();
                }

                //  Currency Parser
                int currencyStartPos = data.IndexOf("currency\":\"") + 11;
                string currencyStrTemp = data.Substring(currencyStartPos, data.IndexOf("\"", currencyStartPos) - currencyStartPos);
                dataTempList.Add(currencyStrTemp);

                //  Timestamp Parser
                int timestampStartPos = data.IndexOf("timestamp\":[") + 12;
                string timestampStrTemp = data.Substring(timestampStartPos, data.IndexOf("],", timestampStartPos) - timestampStartPos);
                dataTempList.Add(timestampStrTemp);

                //  Data Parser
                int dataStartPos = data.IndexOf("close\":[") + 8;
                string dataStrTemp = data.Substring(dataStartPos, data.IndexOf("],", dataStartPos) - dataStartPos);
                dataTempList.Add(dataStrTemp);

                return dataTempList;
            }
            catch (Exception)
            {

                return new List<string>();
            }
            
        }
        private static bool SaveDataToDatabase(string data, List<string> Symbols)
        {
            //  Parse The input data
            List<string> dataParsedList = ParseData(data, Symbols);
            try
            {
                //  Connect to database
                System.Data.SqlClient.SqlConnection con;
                con = new System.Data.SqlClient.SqlConnection();
                con.ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\BIN\\DEBUG\\STOCKSDB.MDF;Integrated Security=True;Connect Timeout=30;";
                con.Open();
                Console.WriteLine("Connection opened!");

                //  Check The data with database if they exists only symbol needed here!
                int dataExists;
                string sql = "SELECT COUNT(*) FROM Company WHERE Symbol like @symbol";
                using(System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@symbol",System.Data.SqlDbType.VarChar, 50).Value = dataParsedList.ElementAt(0);
                    dataExists = (int)cmd.ExecuteScalar();
                }
                //  Save the data
                if(dataExists == 1)
                {
                    sql = "UPDATE Company SET Currency = @currency, TimeStamp = @timestamp, Data = @data WHERE Symbol like @symbol";
                }
                else
                {
                    sql = "INSERT INTO Company (Symbol, Currency, TimeStamp, Data) VALUES(@symbol, @currency, @timestamp, @data)";
                }
                using(System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@symbol", dataParsedList.ElementAt(0));
                    cmd.Parameters.AddWithValue("@currency", dataParsedList.ElementAt(1));
                    cmd.Parameters.AddWithValue("@timestamp", dataParsedList.ElementAt(2));
                    cmd.Parameters.AddWithValue("@data", dataParsedList.ElementAt(3));

                    cmd.ExecuteNonQuery();
                }
                //  Close database and return success
                con.Close();
                Console.WriteLine("Connection closed!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Saving to database!! Error: " + ex);
                return false;
            }
            

            

            

            
        }
        public static string GetDataFromServer(string url)
        {
            
            try
            {
                string html = string.Empty;
                //System.Net.ServicePointManager.Expect100Continue = false;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                request.Timeout = 5000;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                
                using (var response = request.GetResponseAsync())
                {
                    
                    using (var stream = response.Result)
                    {

                        using (var reader = new StreamReader(stream.GetResponseStream()))
                        {
                            html = reader.ReadToEnd();
                        }
                    }
                }
                
                return html;
            }
            catch ( Exception ex)
            {
                return "";
                Console.WriteLine(ex);
            }
            
        }
    }
}
