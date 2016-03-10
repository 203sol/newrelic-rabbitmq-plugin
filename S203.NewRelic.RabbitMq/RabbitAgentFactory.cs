using System.Collections.Generic;
using NewRelic.Platform.Sdk;
using NewRelic.Platform.Sdk.Utils;

namespace S203.NewRelic.RabbitMq
{
    public class RabbitAgentFactory : AgentFactory
    {
        private static readonly Logger Logger = Logger.GetLogger("RabbitLogger");

        public override Agent CreateAgentWithConfiguration(IDictionary<string, object> properties)
        {
            Logger.Info("Initializing Agent...");

            var name = (string)properties["name"];
            var host = (string)properties["host"];
            var port = int.Parse(properties["port"].ToString());
            var username = (string)properties["username"];
            var password = (string)properties["password"];

            return new RabbitAgent(name, host, port, username, password);
        }
    }
}