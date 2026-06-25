namespace AppAssessment.DomainObjects.DTOs
{
    public class AssessmentSessionResponse
    {
        public int SessionId { get; set; }
        public string Status { get; set; } = "";
        public bool IsReadyForAssessment { get; set; }
        public List<string> MissingInformationAreas { get; set; } = new();
        public List<string> NextQuestions { get; set; } = new();
        public AssessmentResponse? FinalAssessment { get; set; } = null;
    }
}