using System.Linq;
using System.Threading.Tasks;

namespace BinanceHistoryDownloader
{
    public class Application
    {
        private IBinanceCsvWriter Writer { get; }

        #region Constructors

        public Application(IBinanceCsvWriter writer)
        {
            Writer = writer;
        }

        #endregion

        #region Methods

        public async Task Run(string[] args)
        {
            var official = false;
            if (args.Any())
            {
                bool.TryParse(args[0], out official);
            }
            
            await Writer.WriteDeposits(official);
            await Writer.WriteWithdrawals(official);
            await Writer.WriteTrades(official);
            if (!official)
            {
                await Writer.WriteDistribution();
                await Writer.WriteDustLog();
            }
        }

        #endregion
    }
}