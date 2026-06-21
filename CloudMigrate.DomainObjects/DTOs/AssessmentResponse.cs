namespace CloudMigrate.DomainObjects.DTOs
{
    public class AssessmentResponse
    {
        public string ExecutiveSummary { get; set; } = "";
        public List<string> RecommendedServices { get; set; } = new();
        public Risk Risks { get; set; } = new();
        public List<string> Tradeoffs { get; set; } = new();
        public List<string> Roadmap { get; set; } = new();
    }

    public class Risk
    {
        public string Mitigation { get; set; } = "";
        public string Impact { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Recommendation { get; set; } = "";
    }
}