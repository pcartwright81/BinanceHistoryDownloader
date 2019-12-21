using Binance.Net.Objects;
using CsvHelper.Configuration;

namespace BinanceHistoryDownloader.Models
{
    internal sealed class TradeHistoryClassMap : ClassMap<BinanceTrade>
    {
        public TradeHistoryClassMap()
        {
            Map(m => m.Time).Name("Date(UTC)");
            Map(m => m.Symbol).Name("Market");
            Map(m => m.IsBuyer).TypeConverter<TradeTypeConverter>().Name("Type");
            Map(m => m.Price).Name("Price");
            Map(m => m.Quantity).Name("Amount");
            Map(m => m.Commission).Name("Total Fee");
            Map(m => m.CommissionAsset).Name("Fee Coin");
        }
    }
}