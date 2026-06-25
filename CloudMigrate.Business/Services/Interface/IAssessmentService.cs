using CloudMigrate.DomainObjects.DBOs;
using CloudMigrate.DomainObjects.DTOs;

namespace CloudMigrate.Business.Services.Interface
{
    public interface IAssessmentService
    {
        Task<List<AssessmentResponse>> GetAssessments();
        Task<AssessmentResponse> GetAssessmentById(int id);
        Task<AssessmentSession> CreateAssessmentSession(AssessmentRequest request);
        Task<AssessmentSessionResponse> GenerateAssessment(AssessmentRequest request);
        Task<AssessmentSessionResponse> SubmitAnswers(SubmitAnswersRequest request);
        Task<bool> UpdateAssessmentSession(AssessmentSession sessionCreated, AssessmentReadinessResponse readiness);
        Task<AssessmentReadinessResponse> ValidateAssessmentRequest(string consolidatedPrompt, List<QuestionAnswer> previousQA);
        Task<string> BuildConsolidatedPrompt(string originalRequest, List<QuestionAnswer> qa);
        Task<AssessmentResponse> GenerateFinalAssessment(string consolidatedPrompt);
        Task<bool> PersistFinalAssessment(AssessmentResponse response, string originalRequest);
    }
}
