namespace ArchitectAI.DomainObjects.DTOs
{
    public class AssessmentResponse
    {
        public string ExecutiveSummary { get; set; } = "";
        public List<string> RecommendedServices { get; set; } = new();
        public List<string> Risks { get; set; } = new();
        public List<string> Tradeoffs { get; set; } = new();
        public List<string> Roadmap { get; set; } = new();
    }
}