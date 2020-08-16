using System.Threading.Tasks;

namespace BinanceHistoryDownloader
{
    public interface IBinanceCsvWriter
    {
        Task WriteDeposits(bool official);

        Task WriteWithdrawals(bool official);

        Task WriteTrades(bool official);

        Task WriteDustLog();

        Task WriteDistribution();
    }
}