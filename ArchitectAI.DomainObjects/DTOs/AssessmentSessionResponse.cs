namespace ArchitectAI.DomainObjects.DTOs
{
    public class AssessmentSessionResponse
    {
        public string SessionId { get; set; }
        public string Status { get; set; } = "";
        public bool IsReadyForAssessment { get; set; }
        public List<string> MissingInformationAreas { get; set; } = new();
        public List<string> NextQuestions { get; set; } = new();
        public AssessmentResponse? FinalAssessment { get; set; } = null;
    }
}