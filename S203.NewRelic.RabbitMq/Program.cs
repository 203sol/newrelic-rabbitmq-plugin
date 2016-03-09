using System;
using NewRelic.Platform.Sdk;
using NewRelic.Platform.Sdk.Utils;

namespace S203.NewRelic.RabbitMq
{
    internal class Program
    {
        private static readonly Logger Logger = Logger.GetLogger("NeuronEsbLogger");

        private static int Main(string[] args)
        {
            Logger.Info("Starting Plugin");
            try
            {
                var runner = new Runner();
                runner.Add(new EsbAgentFactory());
                runner.SetupAndRun();
            }
            catch (Exception e)
            {
                Logger.Error("Exception occurred, unable to continue.\n{0}",e.Message);
                return -1;
            }

            Logger.Info("Exiting Plugin");
            return 0;
        }
    }
}
