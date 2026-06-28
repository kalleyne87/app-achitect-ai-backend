using Newtonsoft.Json;

namespace ArchitectAI.DomainObjects.DBOs
{
    public class QuestionAnswer
    {
        [JsonProperty("question")]
        public string Question { get; set; } = "";

        [JsonProperty("answer")]
        public string Answer { get; set; } = "";

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    }
}