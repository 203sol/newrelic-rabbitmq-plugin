using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using NewRelic.Platform.Sdk;
using NewRelic.Platform.Sdk.Utils;
using Newtonsoft.Json;

namespace S203.NewRelic.RabbitMq
{
    public class RabbitAgent : Agent
    {
        private static readonly Logger Logger = Logger.GetLogger("RabbitMqLogger");
        private readonly Version _version = Assembly.GetExecutingAssembly().GetName().Version;
        private readonly string _name;
        private static string _host;
        private static string _vhost;
        private static int _port;
        private static string _username;
        private static string _password;

        public RabbitAgent(string name, string host, string vhost, int port, string username, string password)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Name must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException(nameof(host), "Host must be specified for the agent to initialize");
            if (string.IsNullOrEmpty(vhost))
                throw new ArgumentNullException(nameof(vhost), "Virtual Host must be specified for the agent to initialize");

            _name = name;
            _host = host;
            _vhost = vhost;
            _port = port;
            _username = username;
            _password = password;
        }

        public override string Guid => "com.203sol.newrelic.rabbitmq";
        public override string Version => _version.Major + "." + _version.Minor + "." + _version.Build;

        public override string GetAgentName()
        {
            return _name;
        }

        public override void PollCycle()
        {
            // Assemble URIs
            var rabbitUri = $"http://{_host}:{_port}/api/";

            // Get Overview Metrics
            var client = new WebClient();
            client.Headers.Add("content-Type", "Application/json");

            Logger.Debug("Getting Overview");

            var result = client.DownloadString(rabbitUri + "overview");
            


            Logger.Debug("Deserializing Neuron Endpoint Health");
            var data = JsonConvert.DeserializeObject<List<EndpointHealth>>(result);
            Logger.Debug("Response received:\n" + data);

            Logger.Debug("Sending Summary Metrics to New Relic");
            ReportMetric("Summary/Heartbeat", "checks", data.Sum(d => d.Heartbeats));
            ReportMetric("Summary/Error", "messages", data.Sum(d => d.Errors));
            ReportMetric("Summary/Warning", "messages", data.Sum(d => d.Warnings));
            ReportMetric("Summary/MessageRate", "messages", (float)data.Sum(d => d.MessageRate));
            ReportMetric("Summary/MessagesProcessed", "messages", data.Sum(d => d.MessagesProcessed));

            Logger.Debug("Sending Individual Metrics to New Relic");
            foreach (var endpoint in data)
            {
                ReportMetric("Heartbeat/" + endpoint.Name, "checks", endpoint.Heartbeats);
                ReportMetric("Error/" + endpoint.Name, "messages", endpoint.Errors);
                ReportMetric("Warning/" + endpoint.Name, "messages", endpoint.Warnings);
                ReportMetric("MessageRate/" + endpoint.Name, "messages", endpoint.Heartbeats);
                ReportMetric("MessagesProcessed/" + endpoint.Name, "messages", endpoint.Heartbeats);
            }
        }
    }
}

/*
Metrics to pull

    def poll_cycle
      response = conn.get("/api/overview")

      statistics = response.body

      report_metric "Queues/Queued", "Messages", statistics.fetch("queue_totals").fetch("messages")
      report_metric "Queues/Ready", "Messages", statistics.fetch("queue_totals").fetch("messages_ready")
      report_metric "Queues/Unacknowledged", "Messages", statistics.fetch("queue_totals").fetch("messages_unacknowledged")

      statistics.fetch("object_totals").each do |key, value|
        report_metric "Objects/#{key.capitalize}", key, value
      end

      report_metric "Messages/Publish", "Messages/Second", @messages_published.process(statistics.fetch("message_stats").fetch("publish"))
      report_metric "Messages/Ack", "Messages/Second", @messages_acked.process(statistics.fetch("message_stats").fetch("ack"))
      report_metric "Messages/Deliver", "Messages/Second", @messages_delivered.process(statistics.fetch("message_stats").fetch("deliver_get"))
      report_metric "Messages/Confirm", "Messages/Second", @messages_confirmed.process(statistics.fetch("message_stats").fetch("confirm"))
      report_metric "Messages/Redeliver", "Messages/Second", @messages_redelivered.process(statistics.fetch("message_stats").fetch("redeliver"))
      report_metric "Messages/NoAck", "Messages/Second", @messages_noacked.process(statistics.fetch("message_stats").fetch("get_no_ack"))

      response = conn.get("/api/nodes")
      statistics = response.body
      statistics.each do |node|
        report_metric "Node/MemoryUsage/#{node.fetch("name")}", "Percentage", (node.fetch("mem_used").to_f / node.fetch("mem_limit"))
        report_metric "Node/ProcUsage/#{node.fetch("name")}", "Percentage", (node.fetch("proc_used").to_f / node.fetch("proc_total"))
        report_metric "Node/FdUsage/#{node.fetch("name")}", "Percentage", (node.fetch("fd_used").to_f / node.fetch("fd_total"))
        report_metric "Node/Type/#{node.fetch("name")}", "Type", node.fetch("type")
        report_metric "Node/Running/#{node.fetch("name")}", "Running", node.fetch("running") ? 1 : 0
      end
    end
    */
