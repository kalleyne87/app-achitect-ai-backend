using Newtonsoft.Json;

namespace ArchitectAI.DomainObjects.DBOs
{
    public class AssessmentSession
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("originalRequest")]
        public string OriginalRequest { get; set; } = "";

        [JsonProperty("collectedQuestionsAndAnswers")]
        public List<QuestionAnswer> CollectedQuestionsAndAnswers { get; set; } = new();

        [JsonProperty("currentQuestions")]
        public List<string> CurrentQuestions { get; set; } = new();

        [JsonProperty("consolidatedPrompt")]
        public string? ConsolidatedPrompt { get; set; }

        [JsonProperty("finalAssessment")]
        public Assessment? FinalAssessment { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "NeedsMoreInformation";

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;

        [JsonProperty("updatedDateTime")]
        public DateTime UpdatedDateTime { get; set; } = DateTime.UtcNow;
    }
}