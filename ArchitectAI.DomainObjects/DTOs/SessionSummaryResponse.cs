namespace ArchitectAI.DomainObjects.DTOs
{
    public class SessionSummaryResponse
    {
        public string Id { get; set; } = "";
        public string OriginalRequest { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedDateTime { get; set; }
    }
}