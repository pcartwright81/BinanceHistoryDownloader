using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.Objects.Spot.WalletData;
using BinanceHistoryDownloader.Models;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.RateLimiter;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinanceHistoryDownloader
{
    public class BinanceCsvWriter : IBinanceCsvWriter
    {
        public BinanceCsvWriter(IOptions<BinanceKeys> binanceKeys, ILogger<BinanceCsvWriter> logger)
        {
            var keys = binanceKeys.Value;
            Logger = logger;
            Client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials(keys.ApiKey, keys.ApiSecret),
                AutoTimestamp = true,
                RateLimiters = new List<IRateLimiter>
                {
                    new RateLimiterPerEndpoint(1000, TimeSpan.FromMinutes(1))
                }
            });
        }

        public BinanceClient Client { get; set; }

        private ILogger<BinanceCsvWriter> Logger { get; }

        public async Task WriteDeposits(bool official)
        {
            Logger.LogDebug("Starting Deposits");
            var endDate = new DateTime(2019, 07, 31);
            var startDate = endDate.AddDays(-90);
            var deposits = new List<BinanceDeposit>();
            while (true)
            {
                if (endDate < new DateTime(2017, 07, 01)) break;
                var hist = await Client.WithdrawDeposit.GetDepositHistoryAsync(null, null, startDate, endDate);
                if (!hist.Success)
                {
                    Logger.LogError(hist.Error?.ToString());
                    return;
                }
                deposits.AddRange(hist.Data);
                endDate = startDate.AddDays(-91);
                startDate = endDate.AddDays(-90);
            }

            if (official)
                WriteCsv(deposits, "Binance_DepositHistory.csv", new DepositClassMap());
            else
                WriteCsv(deposits, "Binance_DepositHistoryRaw.csv", null);

            Logger.LogDebug("Finished Deposits");
        }

        public async Task WriteWithdrawals(bool official)
        {
            Logger.LogDebug("Starting Withdrawals");
            var binanceResult = await Client.WithdrawDeposit.GetWithdrawalHistoryAsync();
            if (!binanceResult.Success)
            {
                Logger.LogError(binanceResult.Error?.ToString());
                return;
            }
            var withdrawals = binanceResult.Data.OrderBy(c => c.ApplyTime).ToList();
            if (official)
                WriteCsv(withdrawals, "Binance_WithdrawalHistory.csv", new WithdrawalClassMap());
            else
                WriteCsv(withdrawals, "Binance_WithdrawalHistoryRaw.csv", null);
            Logger.LogDebug("Finished Withdrawals");
        }

        public async Task WriteTrades(bool official)
        {
            Logger.LogDebug("Starting Trades");
            var trades = new List<BinanceTrade>();
            var binanceResult = await Client.Spot.System.GetExchangeInfoAsync();
            if (!binanceResult.Success)
            {
                Logger.LogError(binanceResult.Error?.ToString());
                return;
            }
            var markets = binanceResult.Data.Symbols.OrderBy(c => c.Name);
            foreach (var market in markets)
                DownloadTrades(trades, market);

            if (official)
                WriteCsv(trades.OrderBy(c => c.TradeTime), "Binance_TradeHistory.csv", new TradeHistoryClassMap());
            else
                WriteCsv(trades.OrderBy(c => c.TradeTime), "Binance_TradeHistoryRaw.csv", null);
            Logger.LogDebug("Finished Trades");
        }

        public async Task WriteDustLog()
        {
            Logger.LogDebug("Starting DustLog");
            var binanceResult = await Client.General.GetDustLogAsync();
            if (!binanceResult.Success)
            {
                Logger.LogError(binanceResult.Error?.ToString());
                return;
            }
            var dustLog = binanceResult.Data;
            var dustLogDetails = new List<BinanceDustLogDetails>();
            foreach (var log in dustLog) dustLogDetails.AddRange(log.Logs);
            WriteCsv(dustLogDetails.OrderBy(c => c.OperateTime), "Binance_DustLog.csv", null);
            Logger.LogDebug("Finished DustLog");
        }

        public async Task WriteDistribution()
        {
            Logger.LogDebug("Starting Distributions");
            var binanceResult = await Client.General.GetAssetDividendRecordsAsync();
            if (!binanceResult.Success)
            {
                Logger.LogError(binanceResult.Error?.ToString());
                return;
            }
            var distribution = binanceResult.Data.Rows.OrderBy(c => c.Timestamp);
            WriteCsv(distribution, "Binance_DistributionHistory.csv", null);
            Logger.LogDebug("Finished Withdrawals");
        }

        private void DownloadTrades(List<BinanceTrade> trades, BinanceSymbol market)
        {
            try
            {
                Logger.LogDebug($"Getting History from {market.Name}");
                var tradeResponse = Client.Spot.Order.GetMyTrades(market.Name);
                if (tradeResponse.Success)
                {
                    trades.AddRange(tradeResponse.Data);
                }
                else
                {
                    Logger.LogError(tradeResponse.Error?.ToString());
                    Thread.Sleep(3000);
                    DownloadTrades(trades, market);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"Getting History From {market.Name} Failed {ex.Message}");
            }
        }

        private static void WriteCsv<T>(IEnumerable<T> records, string csvName, ClassMap classMap)
        {
            using var writer = new StreamWriter(csvName);
            using var csv = new CsvWriter(writer, CultureInfo.CurrentCulture);
            if (classMap != null) csv.Context.RegisterClassMap(classMap);
            csv.WriteRecords(records);
        }
    }
}