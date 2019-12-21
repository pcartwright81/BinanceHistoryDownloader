using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace BinanceHistoryDownloader.Models
{
    internal class TradeTypeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return text == "False" ? "SELL" : "BUY";
        }
    }
}