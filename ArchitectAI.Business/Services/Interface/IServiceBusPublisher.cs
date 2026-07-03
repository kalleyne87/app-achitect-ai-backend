using ArchitectAI.DomainObjects.DBOs;
using ArchitectAI.DomainObjects.DTOs;

namespace ArchitectAI.Business.Services.Interface
{
    public interface IServiceBusPublisher
    {
        Task PublishAssessmentRequestAsync(AssessmentQueueMessage message);
    }
}
