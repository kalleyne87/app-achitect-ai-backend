using CloudMigrate.DomainObjects.DTOs;

namespace CloudMigrate.Business.Services.Interface
{
    public interface IAssessmentService
    {
        Task<AssessmentResponse> GenerateAssessment(AssessmentRequest request);
    }
}
