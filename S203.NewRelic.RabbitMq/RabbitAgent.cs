using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using NewRelic.Platform.Sdk;
using NewRelic.Platform.Sdk.Processors;
using NewRelic.Platform.Sdk.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace S203.NewRelic.RabbitMq
{
    public class RabbitAgent : Agent
    {
        private static readonly Logger Logger = Logger.GetLogger("RabbitLogger");
        private readonly Version _version = Assembly.GetExecutingAssembly().GetName().Version;
        private readonly string _name;
        private static string _host;
        private static int _port;
        private static string _username;
        private static string _password;

        private readonly IProcessor _messagesPublished;
        private readonly IProcessor _messagesAcked;
        private readonly IProcessor _messagesDelivered;
        private readonly IProcessor _messagesConfirmed;
        private readonly IProcessor _messagesRedelivered;
        private readonly IProcessor _messagesNoacked;

        public RabbitAgent(string name, string host, int port, string username, string password)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Name must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException(nameof(host), "Host must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username), "Username must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password), "Password must be specified for the agent to initialize");

            _name = name;
            _host = host;
            _port = port;
            _username = username;
            _password = password;

            // Start the counters
            _messagesPublished = new EpochProcessor();
            _messagesAcked = new EpochProcessor();
            _messagesDelivered = new EpochProcessor();
            _messagesConfirmed = new EpochProcessor();
            _messagesRedelivered = new EpochProcessor();
            _messagesNoacked = new EpochProcessor();
        }

        public override string Guid => "com.203sol.newrelic.rabbitmq";
        public override string Version => _version.Major + "." + _version.Minor + "." + _version.Build;

        public override string GetAgentName()
        {
            return _name;
        }

        public override void PollCycle()
        {
            // Prep the client
            var rabbitUri = $"http://{_host}:{_port}/api/";
            var client = new WebClient { Credentials = new NetworkCredential(_username, _password) };
            client.Headers.Add("content-Type", "Application/json");

            // ------- Overview ---------

            Logger.Debug("Getting Overview");

            var result = client.DownloadString(rabbitUri + "overview");

            Logger.Debug("Deserializing RabbitMQ Overview");

            var overview = JObject.Parse(result);
            Logger.Debug("Response received:\n" + overview);

            Logger.Debug("Sending Queue Summary Metrics to New Relic");

            ReportMetric("Queues/Queued", "Messages", overview["queue_totals"]["messages"]?.Value<int>() ?? 0);
            ReportMetric("Queues/Ready", "Messages", overview["queue_totals"]["messages_ready"]?.Value<int>() ?? 0);
            ReportMetric("Queues/Unacknowledged", "Messages", overview["queue_totals"]["messages_unacknowledged"]?.Value<int>() ?? 0);

            Logger.Debug("Sending Object Summary Metrics to New Relic");

            var objects = JsonConvert.DeserializeObject<Dictionary<string, int>>(overview["object_totals"].ToString());
            foreach (var metric in objects)
            {
                ReportMetric("Objects/" + metric.Key, metric.Key, metric.Value);
            }

            Logger.Debug("Sending Message Summary Metrics to New Relic");

            ReportMetric("Messages/Publish", "Messages/Second",
                _messagesPublished.Process(overview["message_stats"]["publish"]?.Value<int>() ?? 0));

            ReportMetric("Messages/Ack", "Messages/Second",
                _messagesAcked.Process(overview["message_stats"]["ack"]?.Value<int>() ?? 0));

            ReportMetric("Messages/Deliver", "Messages/Second",
                _messagesDelivered.Process(overview["message_stats"]["deliver"]?.Value<int>() ?? 0));

            ReportMetric("Messages/Confirm", "Messages/Second",
                _messagesConfirmed.Process(overview["message_stats"]["confirm"]?.Value<int>() ?? 0));

            ReportMetric("Messages/Redeliver", "Messages/Second",
                _messagesRedelivered.Process(overview["message_stats"]["redeliver"]?.Value<int>() ?? 0));

            ReportMetric("Messages/NoAck", "Messages/Second",
                _messagesNoacked.Process(overview["message_stats"]["get_noack"]?.Value<int>() ?? 0));


            // ------- Nodes ---------

            Logger.Debug("Getting Nodes");

            result = client.DownloadString(rabbitUri + "nodes");

            Logger.Debug("Deserializing RabbitMQ Nodes");

            var node = JArray.Parse(result)[0];

            Logger.Debug("Response received:\n" + node);

            Logger.Debug("Sending Node Metrics to New Relic");

            ReportMetric("Node/DiskUsage/" + node["name"], "Bytes", node["disk_free"]?.Value<long>() ?? 0);
            ReportMetric("Node/MemoryUsage/" + node["name"], "Percentage", node["mem_used"]?.Value<int>() ?? 0 / node["mem_limit"]?.Value<int>() ?? 0);
            ReportMetric("Node/ProcUsage/" + node["name"], "Percentage", node["proc_used"]?.Value<int>() ?? 0 / node["proc_total"]?.Value<int>() ?? 0);
            ReportMetric("Node/FileDescUsage/" + node["name"], "Percentage", node["fd_used"]?.Value<int>() ?? 0 / node["fd_total"]?.Value<int>() ?? 0);
            ReportMetric("Node/SocketUsage/" + node["name"], "Percentage", node["sockets_used"]?.Value<int>() ?? 0 / node["sockets_total"]?.Value<int>() ?? 0);
            ReportMetric("Node/Running/" + node["name"], "Running", node["running"]?.Value<bool>() ?? false ? 1 : 0);
        }
    }
}