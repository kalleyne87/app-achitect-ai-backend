
namespace CloudMigrate.DomainObjects.DBOs
{
    public class Assessment
    {
        public int Id { get; set; }
        public string Requirements { get; set; } = "";
        public string ExecutiveSummary { get; set; } = "";
        public string RecommendedServicesJson { get; set; } = "";
        public string RisksJson { get; set; } = "";
        public string TradeoffsJson { get; set; } = "";
        public string RoadmapJson { get; set; } = "";
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    }
}
