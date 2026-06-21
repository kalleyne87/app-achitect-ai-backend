using CloudMigrate.DomainObjects.DBOs;
using CloudMigrate.DomainObjects.DTOs;

namespace CloudMigrate.Business.Services.Interface
{
    public interface IAssessmentService
    {
        Task<List<Assessment>> GetAssessments();
        Task<Assessment> GetAssessmentById(int id);
        Task<bool> CreateAssessment(AssessmentResponse request);
        Task<AssessmentResponse> GenerateAssessment(AssessmentRequest request);
    }
}
