using Newtonsoft.Json;

namespace ArchitectAI.DomainObjects.DBOs
{
    public class Assessment
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("requirements")]
        public string Requirements { get; set; } = "";

        [JsonProperty("executiveSummary")]
        public string ExecutiveSummary { get; set; } = "";

        [JsonProperty("recommendedServices")]
        public List<string> RecommendedServices { get; set; } = new();

        [JsonProperty("risks")]
        public List<string> Risks { get; set; } = new();

        [JsonProperty("tradeoffs")]
        public List<string> Tradeoffs { get; set; } = new();

        [JsonProperty("roadmap")]
        public List<string> Roadmap { get; set; } = new();

        [JsonProperty("sessionId")]
        public string SessionId { get; set; } = "";

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    }
}