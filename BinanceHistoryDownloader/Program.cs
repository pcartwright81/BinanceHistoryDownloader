using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.RateLimiter;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Configuration;
namespace BinanceHistoryDownloader
{
    internal class Program
    {
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddUserSecrets<BinanceKeys>();
            IConfiguration Configuration = builder.Build();
            var binanceKeys = Configuration.GetSection("BinanceKeys").GetChildren();
            string apiKey = null;
            string apiSecret = null;
            foreach(var binanceKey in binanceKeys)
            {
                if(binanceKey.Key == "APIKey")
                {
                    apiKey = binanceKey.Value;
                }
                else
                {
                    apiSecret = binanceKey.Value;
                }
            }
            ApiCredentials ApiCredentials = new ApiCredentials(apiKey, apiSecret);
            IRateLimiter rateLimiter = new RateLimiterPerEndpoint(1000, TimeSpan.FromMinutes(1));
            List<IRateLimiter> limiters = new List<IRateLimiter>
            {
                rateLimiter
            };
            BinanceClientOptions options = new BinanceClientOptions
            {
                ApiCredentials = ApiCredentials,
                AutoTimestamp = true,
                RateLimiters = limiters
            };

            var client = new BinanceClient(options);
            Console.WriteLine("Getting Deposits");
            var deposits = client.GetDepositHistory().Data.OrderBy(c => c.InsertTime).ToList();
            WriteCsv(deposits, "Binance_DepositHistory.csv", new DepositClassMap());
            WriteCsv(deposits, "Binance_DepositHistoryRaw.csv", null);
            Console.WriteLine("Getting Withdrawals");
            var withdrawals = client.GetWithdrawalHistory().Data.OrderBy(c => c.ApplyTime).ToList();
            WriteCsv(withdrawals, "Binance_WithdrawalHistory.csv", new WithdrawalClassMap());
            WriteCsv(withdrawals, "Binance_WithdrawalHistoryRaw.csv", null);
            Console.WriteLine("Getting Distributions");
            var distribution = client.GetAssetDividendRecords().Data.Rows.OrderBy(c => c.Timestamp);
            WriteCsv(distribution, "Binance_DistributionHistory.csv", null);
            Console.WriteLine("Getting DustLog");
            var dustLog = client.GetDustLog().Data;
            var dustLogDetails = new List<BinanceDustLogDetails>();
            foreach (var log in dustLog) dustLogDetails.AddRange(log.Logs);
            WriteCsv(dustLogDetails.OrderBy(c => c.OperateTime), "Binance_DustLog.csv", null);
            var trades = new List<BinanceTrade>();
            var markets = client.GetExchangeInfo().Data.Symbols.OrderBy(c => c.Name);
            foreach (var market in markets)
                DownloadTrades(client, trades, market);


            WriteCsv(trades.OrderBy(c => c.Time), "Binance_TradeHistory.csv", new TradeHistoryClassMap());
            WriteCsv(trades.OrderBy(c => c.Time), "Binance_TradeHistoryRaw.csv", null);
        }

        private static void DownloadTrades(BinanceClient client, List<BinanceTrade> trades, BinanceSymbol market)
        {
            try
            {
                Console.WriteLine("Getting History From " + market.Name);
                var tradeResponse = client.GetMyTrades(market.Name);
                if (tradeResponse.Success)
                {
                    trades.AddRange(tradeResponse.Data);
                }
                else
                {
                    Console.WriteLine(tradeResponse.Error);
                    Thread.Sleep(1000);
                    DownloadTrades(client, trades, market);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Getting History From " + market.Name + " Failed " + ex.Message);
            }

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