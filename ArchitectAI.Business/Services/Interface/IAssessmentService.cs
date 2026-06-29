using ArchitectAI.DomainObjects.DBOs;
using ArchitectAI.DomainObjects.DTOs;

namespace ArchitectAI.Business.Services.Interface
{
    public interface IAssessmentService
    {
        Task<List<AssessmentResponse>> GetAssessments();
        Task<AssessmentResponse> GetAssessmentById(string id);
        Task<AssessmentSession> CreateAssessmentSession(AssessmentRequest request);
        Task<AssessmentSessionResponse> GenerateAssessment(AssessmentRequest request);
        Task<AssessmentSessionResponse> SubmitAnswers(SubmitAnswersRequest request);
        Task UpdateAssessmentSession(AssessmentSession session, AssessmentReadinessResponse readiness);
        Task<AssessmentReadinessResponse> ValidateAssessmentRequest(string consolidatedPrompt, List<QuestionAnswer> previousQA);
        Task<string> BuildConsolidatedPrompt(string originalRequest, List<QuestionAnswer> qa);
        Task<AssessmentResponse> GenerateFinalAssessment(string consolidatedPrompt);
        Task PersistFinalAssessment(AssessmentResponse response, string originalRequest, string sessionId);
        Task<AssessmentSession> GetSessionById(string id);
        Task<List<SessionSummaryResponse>> GetAllSessions();
    }
}
