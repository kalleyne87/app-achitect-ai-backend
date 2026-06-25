namespace AppAssessment.DomainObjects.DBOs
{
    public class QuestionAnswerItem
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    }
}