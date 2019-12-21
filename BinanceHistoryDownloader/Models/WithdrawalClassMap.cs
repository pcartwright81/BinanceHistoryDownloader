using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Binance.Net.Objects;
using CsvHelper.Configuration;

namespace BinanceHistoryDownloader.Models
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public sealed class WithdrawalClassMap : ClassMap<BinanceWithdrawal>
    {
        public WithdrawalClassMap()
        {
            Map(m => m.ApplyTime).Name("Date");
            Map(m => m.Asset).Name("Coin");
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
