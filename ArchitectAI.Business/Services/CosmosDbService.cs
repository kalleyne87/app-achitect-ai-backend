using ArchitectAI.Business.Services.Interface;
using ArchitectAI.DomainObjects.DBOs;
using ArchitectAI.DomainObjects.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;


namespace ArchitectAI.Business.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly CosmosClient _client;
        private readonly Container _sessionsContainer;
        private readonly Container _assessmentsContainer;
        private readonly ILogger<CosmosDbService> _logger;
        public CosmosDbService(
            IOptions<CosmosDbOptions> options,
            ILogger<CosmosDbService> logger)
        {
            _logger = logger;
            var opt = options.Value;

            _client = new CosmosClient(opt.Endpoint, opt.Key, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });

            var database = _client.GetDatabase(opt.DatabaseName);
            _sessionsContainer = database.GetContainer(opt.SessionsContainer);
            _assessmentsContainer = database.GetContainer(opt.AssessmentsContainer);
        }

        public async Task<AssessmentSession> CreateSessionAsync(AssessmentSession session)
        {
            _logger.LogInformation("Creating Cosmos session {Id}", session.Id);
            var response = await _sessionsContainer.CreateItemAsync(session, new PartitionKey(session.Id));
            return response.Resource;
        }

        public async Task<AssessmentSession?> GetSessionAsync(string id)
        {
            try
            {
                var response = await _sessionsContainer.ReadItemAsync<AssessmentSession>(
                    id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Session {Id} not found in Cosmos.", id);
                return null;
            }
        }

        public async Task<AssessmentSession> UpdateSessionAsync(AssessmentSession session)
        {
            session.UpdatedDateTime = DateTime.UtcNow;
            _logger.LogInformation("Updating Cosmos session {Id}", session.Id);
            var response = await _sessionsContainer.UpsertItemAsync(session, new PartitionKey(session.Id));
            return response.Resource;
        }

        public async Task<Assessment> CreateAssessmentAsync(Assessment assessment)
        {
            _logger.LogInformation("Creating Cosmos assessment {Id}", assessment.Id);
            var response = await _assessmentsContainer.CreateItemAsync(
                assessment, new PartitionKey(assessment.Id));
            return response.Resource;
        }

        public async Task<List<Assessment>> GetAssessmentsAsync()
        {
            var query    = _assessmentsContainer.GetItemQueryIterator<Assessment>(
                "SELECT * FROM c ORDER BY c.createdDateTime DESC");
            var results  = new List<Assessment>();

            while (query.HasMoreResults)
            {
                var page = await query.ReadNextAsync();
                results.AddRange(page);
            }

            return results;
        }

        public async Task<Assessment?> GetAssessmentAsync(string id)
        {
            try
            {
                var response = await _assessmentsContainer.ReadItemAsync<Assessment>(
                    id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Assessment {Id} not found in Cosmos.", id);
                return null;
            }
        }
    }
}