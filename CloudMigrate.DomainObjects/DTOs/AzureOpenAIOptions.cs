namespace CloudMigrate.DomainObjects.DTOs
{
    public class AzureOpenAIOptions
    {
        public string Endpoint { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string DeploymentName { get; set; } = "";
    }
}