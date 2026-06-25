
namespace CloudMigrate.DomainObjects.DBOs
{
    public class AssessmentSession
    {
        public int Id { get; set; }
        public string OriginalRequest { get; set; } = "";
        public string CollectedQuestionsAndAnswersJson { get; set; } = "[]";
        public string? ConsolidatedPrompt { get; set; }
        public string CurrentQuestionsJson { get; set; } = "";
        public string Status { get; set; } = "NeedsMoreInformation";
        public string? FinalAssessmentJson { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDateTime { get; set; }
    }
}
