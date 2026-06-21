using System.Collections.Generic;
using Azure;
using Azure.AI.OpenAI;
using CloudMigrate.Business.Services.Interface;
using CloudMigrate.DomainObjects.DTOs;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace CloudMigrate.Business.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly AzureOpenAIOptions _options;

        public AssessmentService(IOptions<AzureOpenAIOptions> options)
        {
            _options = options.Value;
        }

        public async Task<AssessmentResponse> GenerateAssessment(AssessmentRequest request)
        {
            var client = new AzureOpenAIClient(
                new Uri(_options.Endpoint),
                new AzureKeyCredential(_options.ApiKey));

            var chatClient = client.GetChatClient(_options.DeploymentName);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(
                    """
                    Hello my name is KB.
                    Analyze requirements and provide:
                    - Executive Summary
                    - Recommended Azure Services
                    - Risks
                    - Tradeoffs
                    """
                ),
                
                ChatMessage.CreateUserMessage(request.Requirements)
            };

            var response = await chatClient.CompleteChatAsync(messages);

            return new AssessmentResponse
            {
                ExecutiveSummary = response.Value.Content[0].Text,

                // We'll make these structured later
                RecommendedServices = new List<string>(),
                Risks = new Risk(),
                Tradeoffs = new List<string>()
            };
        }
    }
}