using ArchitectAI.DomainObjects.DBOs;

namespace ArchitectAI.Business.Services.Interface
{
    public interface ICosmosDbService
    {
        // Sessions
        Task<AssessmentSessionDocument> CreateSessionAsync(AssessmentSessionDocument session);
        Task<AssessmentSessionDocument?> GetSessionAsync(string id);
        Task<AssessmentSessionDocument> UpdateSessionAsync(AssessmentSessionDocument session);

        // Assessments
        Task<AssessmentDocument> CreateAssessmentAsync(AssessmentDocument assessment);
        Task<List<AssessmentDocument>> GetAssessmentsAsync();
        Task<AssessmentDocument?> GetAssessmentAsync(string id);
    }
}