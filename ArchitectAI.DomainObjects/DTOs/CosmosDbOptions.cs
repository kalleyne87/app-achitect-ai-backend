namespace ArchitectAI.DomainObjects.DTOs
{
    public class CosmosDbOptions
    {
        public const string SectionName = "CosmosDb";
        public string Endpoint { get; set; } = "";
        public string Key { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public string SessionsContainer { get; set; } = "";
        public string AssessmentsContainer { get; set; } = "";
    }
}