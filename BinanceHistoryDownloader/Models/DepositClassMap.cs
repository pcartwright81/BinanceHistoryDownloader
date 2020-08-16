using Binance.Net.Objects.Spot.WalletData;
using CsvHelper.Configuration;

namespace BinanceHistoryDownloader.Models
{
    public sealed class DepositClassMap : ClassMap<BinanceDeposit>
    {
        public DepositClassMap()
        {
            Map(m => m.InsertTime).Name("Date");
            Map(m => m.Coin).Name("Coin");
            Map(m => m.Amount).Name("Amount");
            Map().Constant(0).Name("TransactionFee");
            Map(m => m.Address).Name("Address");
            Map(m => m.TransactionId).Name("TXID");
            Map().Constant("").Name("SourceAddress");
            Map().Constant("").Name("PaymentID");
            Map(m => m.Status).Name("Status");
        }
    }
}