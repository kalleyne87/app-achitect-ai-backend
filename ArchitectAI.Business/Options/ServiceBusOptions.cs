namespace ArchitectAI.Business.Options
{
    public class ServiceBusOptions
    {
        public const string SectionName = "ServiceBus";
        public string ConnectionString { get; set; } = "";
        public string QueueName { get; set; } = "assessment-queue";
    }
}