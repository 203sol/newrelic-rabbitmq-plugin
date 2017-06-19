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
        private string _scheme;
        private string _host;
        private int _port;
        private string _username;
        private string _password;

        private readonly IProcessor _messagesPublished;
        private readonly IProcessor _messagesAcked;
        private readonly IProcessor _messagesDelivered;
        private readonly IProcessor _messagesConfirmed;
        private readonly IProcessor _messagesRedelivered;
        private readonly IProcessor _messagesNoacked;

        public RabbitAgent(string name, string scheme, string host, int port, string username, string password)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Name must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(scheme))
                throw new ArgumentNullException(nameof(scheme), "Scheme must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException(nameof(host), "Host must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username), "Username must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password), "Password must be specified for the agent to initialize");

            _name = name;
            _host = host;
            _port = port;
            _scheme = scheme;
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
            var rabbitUri = $"{_scheme}://{_host}:{_port}/api/";
            var client = new WebClient { Credentials = new NetworkCredential(_username, _password) };
            client.Headers.Add("content-Type", "Application/json");

            // ------- Overview ---------

            Logger.Debug("Getting Overview Stats");

            var result = client.DownloadString(rabbitUri + "overview");

            Logger.Debug("Deserializing RabbitMQ Overview");

            var overview = JObject.Parse(result);
            Logger.Debug("Response received:\n" + overview);

            Logger.Debug("Sending Queue Summary Metrics to New Relic");

            ReportMetric("Server/Queued", "Messages", overview["queue_totals"]["messages"]?.Value<float>() ?? 0);
            ReportMetric("Server/Ready", "Messages", overview["queue_totals"]["messages_ready"]?.Value<float>() ?? 0);
            ReportMetric("Server/NoAck", "Messages", overview["queue_totals"]["messages_unacknowledged"]?.Value<float>() ?? 0);

            Logger.Debug("Sending Object Summary Metrics to New Relic");

            var objects = JsonConvert.DeserializeObject<Dictionary<string, float>>(overview["object_totals"].ToString());
            foreach (var metric in objects)
            {
                ReportMetric("Objects/" + UppercaseFirst(metric.Key), UppercaseFirst(metric.Key), metric.Value);
            }

            Logger.Debug("Sending Message Summary Metrics to New Relic");

            ReportMetric("Messages/Publish", "Messages/Second",
                _messagesPublished.Process(overview["message_stats"]["publish"]?.Value<float>() ?? 0));

            ReportMetric("Messages/Ack", "Messages/Second",
                _messagesAcked.Process(overview["message_stats"]["ack"]?.Value<float>() ?? 0));

            ReportMetric("Messages/Deliver", "Messages/Second",
                _messagesDelivered.Process(overview["message_stats"]["deliver"]?.Value<float>() ?? 0));

            ReportMetric("Messages/Confirm", "Messages/Second",
                _messagesConfirmed.Process(overview["message_stats"]["confirm"]?.Value<float>() ?? 0));

            ReportMetric("Messages/Redeliver", "Messages/Second",
                _messagesRedelivered.Process(overview["message_stats"]["redeliver"]?.Value<float>() ?? 0));

            ReportMetric("Messages/NoAck", "Messages/Second",
                _messagesNoacked.Process(overview["message_stats"]["get_noack"]?.Value<float>() ?? 0));


            // ------- Nodes ---------

            Logger.Debug("Getting Node Stats");

            result = client.DownloadString(rabbitUri + "nodes");

            Logger.Debug("Deserializing RabbitMQ Nodes");

            var nodes = JArray.Parse(result);

            Logger.Debug("Response received:\n" + nodes);

            Logger.Debug("Sending Node Metrics to New Relic");

            foreach (var node in nodes)
            {
                var diskFree = node["disk_free"]?.Value<float>() ?? 0;
                var memUsed = (node["mem_used"]?.Value<float>() ?? 0) / node["mem_limit"].Value<float>();
                var procUsed = (node["proc_used"]?.Value<float>() ?? 0) / node["proc_total"].Value<float>();
                var fdUsed = (node["fd_used"]?.Value<float>() ?? 0) / node["fd_total"].Value<float>();
                var socketsUsed = (node["sockets_used"]?.Value<float>() ?? 0) / node["sockets_total"].Value<float>();

                Logger.Debug("Disk Free Bytes: " + diskFree);
                Logger.Debug("Memory Used %: " + memUsed);
                Logger.Debug("Processor Used %: " + procUsed);
                Logger.Debug("File Descriptors Used %: " + fdUsed);
                Logger.Debug("Sockets Used %: " + socketsUsed);

                ReportMetric("Node/DiskUsage/" + node["name"], "Bytes", diskFree);
                ReportMetric("Node/MemoryUsage/" + node["name"], "Percentage", memUsed);
                ReportMetric("Node/ProcUsage/" + node["name"], "Percentage", procUsed);
                ReportMetric("Node/FileDescUsage/" + node["name"], "Percentage", fdUsed);
                ReportMetric("Node/SocketUsage/" + node["name"], "Percentage", socketsUsed);
                ReportMetric("Node/Running/" + node["name"], "Running", node["running"]?.Value<bool>() ?? false ? 1 : 0);
            }

            // -------- Queues ---------

            Logger.Debug("Getting Queues");

            result = client.DownloadString(rabbitUri + "queues");

            Logger.Debug("Deserializing RabbitMQ Queues");

            var queues = JArray.Parse(result);

            Logger.Debug("Response received:\n" + nodes);

            Logger.Debug("Sending Node Metrics to New Relic");

            foreach (var queue in queues)
            {
                var vhost = queue["vhost"].Value<string>() == "/" ? "Root" : queue["vhost"];
                ReportMetric($"Queues/{vhost}/{queue["name"]}/Messages/Total", "Messages", queue["messages"]?.Value<float>() ?? 0);
                ReportMetric($"Queues/{vhost}/{queue["name"]}/Messages/Ready", "Messages", queue["messages_ready"]?.Value<float>() ?? 0);
                ReportMetric($"Queues/{vhost}/{queue["name"]}/Messages/NoAck", "Messages", queue["messages_unacknowledged"]?.Value<float>() ?? 0);
                ReportMetric($"Queues/{vhost}/{queue["name"]}/Consumers", "Consumers", queue["consumers"]?.Value<float>() ?? 0);
            }
        }

        private static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}