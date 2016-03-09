using System.Collections.Generic;
using NewRelic.Platform.Sdk;
using NewRelic.Platform.Sdk.Utils;

namespace S203.NewRelic.RabbitMq
{
    public class EsbAgentFactory : AgentFactory
    {
        private static readonly Logger Logger = Logger.GetLogger("NeuronEsbLogger");

        public override Agent CreateAgentWithConfiguration(IDictionary<string, object> properties)
        {
            Logger.Info("Initializing Agent...");

            var name = (string)properties["name"];
            var host = (string)properties["host"];
            var port = int.Parse(properties["port"].ToString());
            var instance = (string)properties["instance"];

            return new EsbAgent(name, host, port, instance);
        }
    }
}
