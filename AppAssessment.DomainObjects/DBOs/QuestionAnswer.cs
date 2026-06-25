
namespace AppAssessment.DomainObjects.DBOs
{
    public class QuestionAnswer
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    }
}
