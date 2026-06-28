using ArchitectAI.DomainObjects.DBOs;
using ArchitectAI.DomainObjects.DTOs;

namespace ArchitectAI.Business.Services.Interface
{
    public interface IAssessmentService
    {
        Task<List<AssessmentResponse>> GetAssessments();
        Task<AssessmentResponse> GetAssessmentById(string id);
        Task<AssessmentSessionDocument> CreateAssessmentSession(AssessmentRequest request);
        Task<AssessmentSessionResponse> GenerateAssessment(AssessmentRequest request);
        Task<AssessmentSessionResponse> SubmitAnswers(SubmitAnswersRequest request);
        Task UpdateAssessmentSession(AssessmentSessionDocument session, AssessmentReadinessResponse readiness);
        Task<AssessmentReadinessResponse> ValidateAssessmentRequest(string consolidatedPrompt, List<QuestionAnswerDocument> previousQA);
        Task<string> BuildConsolidatedPrompt(string originalRequest, List<QuestionAnswerDocument> qa);
        Task<AssessmentResponse> GenerateFinalAssessment(string consolidatedPrompt);
        Task PersistFinalAssessment(AssessmentResponse response, string originalRequest, string sessionId);
    }
}
