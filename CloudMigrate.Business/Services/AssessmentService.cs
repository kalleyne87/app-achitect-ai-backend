using System.Text.Json;
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
                    You are a Principal Azure Cloud Architect.

                    All recommendations should lean toward Microsoft Azure services unless no strong Azure equivalent exists.

                    Only recommend current generally available Azure services. Do not recommend retired, deprecated, preview-only, or end-of-life Azure services. 
                    If a service has been replaced, recommend the current Microsoft-recommended successor.

                    Return 4 to 6 roadmap items only.
                    Each roadmap item must be a single complete sentence.

                    Return ONLY valid JSON.

                    The JSON must match this exact shape: {
                        "executiveSummary": "short summary only",
                        "recommendedServices": [
                            "service 1",
                            "service 2"
                        ],
                        "risks": [
                            "risk 1",
                            "risk 2"
                        ],
                        "tradeoffs": [
                            "tradeoff 1",
                            "tradeoff 2"
                        ],
                        "roadmap": [
                            "step 1",
                            "step 2",
                            "step 3"
                            "step 4",
                            "step 5"
                        ]
                    }

                    Do not include markdown.
                    Do not include extra fields.
                    Do not put all content inside executiveSummary.
                    """
                ),
                
                ChatMessage.CreateUserMessage(request.Requirements)
            };

            var response = await chatClient.CompleteChatAsync(messages);

            var responseText = response.Value.Content[0].Text;

            var assessment = JsonSerializer.Deserialize<AssessmentResponse>(
                responseText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return assessment!;
        }
    }
}