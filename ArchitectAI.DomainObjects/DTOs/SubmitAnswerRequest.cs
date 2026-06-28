using ArchitectAI.DomainObjects.DBOs;

namespace ArchitectAI.DomainObjects.DTOs
{
    public class SubmitAnswersRequest
    {
        public string SessionId { get; set; } = "";
        public List<QuestionAnswer> Answers { get; set; } = new();
    }
}