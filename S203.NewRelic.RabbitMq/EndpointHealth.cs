
namespace S203.NewRelic.RabbitMq
{
    public class EndpointHealth
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public int Heartbeats { get; set; }
        public string LastHeartbeat { get; set; }
        public bool IsMessageSuccess { get; set; }
        public int Errors { get; set; }
        public int Warnings { get; set; }
        public int MessagesProcessed { get; set; }
        public double MessageRate { get; set; }
        public string Hostname { get; set; }
        public string Application { get; set; }
        public string Instance { get; set; }
        public string PrimaryHost { get; set; }
        public string FailureDetails { get; set; }
        public object AgStats { get; set; }
    }
}
