using ArchitectAI.DomainObjects.DBOs;

namespace ArchitectAI.Business.Services.Interface
{
    public interface ICosmosDbService
    {
        // Sessions
        Task<AssessmentSession> CreateSessionAsync(AssessmentSession session);
        Task<AssessmentSession?> GetSessionAsync(string id);
        Task<AssessmentSession> UpdateSessionAsync(AssessmentSession session);
        Task<List<AssessmentSession>> GetAllSessionsAsync();

        // Assessments
        Task<Assessment> CreateAssessmentAsync(Assessment assessment);
        Task<List<Assessment>> GetAssessmentsAsync();
        Task<Assessment?> GetAssessmentAsync(string id);
    }
}