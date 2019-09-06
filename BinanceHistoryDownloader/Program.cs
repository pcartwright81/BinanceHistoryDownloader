using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace BinanceHistoryDownloader
{
    internal class Program
    {
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static void Main(string[] args)
        {
            var options = new BinanceClientOptions
            {
                //todo add key and secret
                ApiCredentials = new ApiCredentials("","")
            };
            var client = new BinanceClient(options);
            Console.WriteLine("Getting Deposits");
            var deposits = client.GetDepositHistory().Data.List.OrderBy(c => c.InsertTime).ToList();
            WriteCsv(deposits, "Binance_DepositHistory.csv", new DepositClassMap());
            WriteCsv(deposits, "Binance_DepositHistoryRaw.csv", null);
            Console.WriteLine("Getting Withdrawals");
            var withdrawals = client.GetWithdrawHistory().Data.List.OrderBy(c => c.ApplyTime).ToList();
            WriteCsv(withdrawals, "Binance_WithdrawalHistory.csv", new WithdrawalClassMap());
            WriteCsv(withdrawals, "Binance_WithdrawalHistoryRaw.csv", null);
            Console.WriteLine("Getting Distributions");
            var distribution = client.GetAssetDividendRecords().Data.Rows.OrderBy(c => c.Timestamp);;
            WriteCsv(distribution, "Binance_DistributionHistory.csv", null);
            Console.WriteLine("Getting DustLog");
            var dustLog = client.GetDustLog().Data;
            var dustLogDetails = new List<BinanceDustLogDetails>();
            foreach (var log in dustLog) dustLogDetails.AddRange(log.Logs);
            WriteCsv(dustLogDetails.OrderBy(c => c.OperateTime), "Binance_DustLog.csv", null);
            var trades = new List<BinanceTrade>();
            var markets = client.GetExchangeInfo().Data.Symbols.OrderBy(c => c.Name);
            foreach (var market in markets)
                try
                {
                    Console.WriteLine("Getting History From " + market.Name);
                    trades.AddRange(client.GetMyTrades(market.Name).Data);
                }
                catch
                {
                    Console.WriteLine("Getting History From " + market.Name + "Failed");
                }

            WriteCsv(trades.OrderBy(c => c.Time), "Binance_TradeHistory.csv", new TradeHistoryClassMap());
            WriteCsv(trades.OrderBy(c => c.Time), "Binance_TradeHistoryRaw.csv", null);
        }


        private static void WriteCsv<T>(IEnumerable<T> records, string csvName, ClassMap classMap)
        {
            using (var writer = new StreamWriter(csvName))
            using (var csv = new CsvWriter(writer))
            {
                if (classMap != null) csv.Configuration.RegisterClassMap(classMap);
                csv.WriteRecords(records);
            }
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public sealed class DepositClassMap : ClassMap<BinanceDeposit>
        {
            public DepositClassMap()
            {
                Map(m => m.InsertTime).Name("Date");
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

    internal class TradeTypeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return text == "False" ? "SELL" : "BUY";
        }
    }
}