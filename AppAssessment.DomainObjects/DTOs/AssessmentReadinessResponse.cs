namespace AppAssessment.DomainObjects.DTOs
{
    public class AssessmentReadinessResponse
    {
        public bool IsReadyForAssessment { get; set; }
        public List<string> MissingInformationAreas { get; set; } = new();
        public List<string> NextQuestions { get; set; } = new();
    }
}