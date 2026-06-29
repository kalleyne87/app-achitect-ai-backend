using System.Text.Json;
using AutoMapper;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Microsoft.Extensions.Logging;
using ArchitectAI.DomainObjects.DTOs;
using ArchitectAI.DomainObjects.DBOs;
using ArchitectAI.Business.Services.Interface;

namespace ArchitectAI.Business.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly AzureOpenAIOptions _options;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IMapper _mapper;
        private readonly ILogger<AssessmentService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions =
            new() { PropertyNameCaseInsensitive = true };

        public AssessmentService(
            IOptions<AzureOpenAIOptions> options,
            ICosmosDbService cosmosDbService,
            IMapper mapper,
            ILogger<AssessmentService> logger)
        {
            _options  = options.Value;
            _cosmosDbService = cosmosDbService;
            _mapper   = mapper;
            _logger   = logger;
        }

        public async Task<List<AssessmentResponse>> GetAssessments()
        {
            try
            {
                var docs = await _cosmosDbService.GetAssessmentsAsync();

                return docs.Select(d => new AssessmentResponse
                {
                    ExecutiveSummary    = d.ExecutiveSummary,
                    RecommendedServices = d.RecommendedServices,
                    Risks               = d.Risks,
                    Tradeoffs           = d.Tradeoffs,
                    Roadmap             = d.Roadmap
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all assessments.");
                throw;
            }
        }

        public async Task<AssessmentResponse> GetAssessmentById(string id)
        {
            try
            {
                var doc = await _cosmosDbService.GetAssessmentAsync(id)
                    ?? throw new KeyNotFoundException($"Assessment with ID {id} not found.");

                return new AssessmentResponse
                {
                    ExecutiveSummary    = doc.ExecutiveSummary,
                    RecommendedServices = doc.RecommendedServices,
                    Risks               = doc.Risks,
                    Tradeoffs           = doc.Tradeoffs,
                    Roadmap             = doc.Roadmap
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assessment {Id}.", id);
                throw;
            }
        }

        public async Task<AssessmentSession> CreateAssessmentSession(AssessmentRequest request)
        {
            var session = new AssessmentSession
            {
                OriginalRequest = request.Requirements,
                CollectedQuestionsAndAnswers = new List<QuestionAnswer>(),
                CurrentQuestions = new List<string>(),
                Status = "NeedsMoreInformation",
                CreatedDateTime = DateTime.UtcNow,
                UpdatedDateTime = DateTime.UtcNow
            };

            var created = await _cosmosDbService.CreateSessionAsync(session);
            _logger.LogInformation("Created Cosmos session {Id}.", created.Id);
            return created;
        }

        public async Task<AssessmentSessionResponse> GenerateAssessment(AssessmentRequest request)
        {
            _logger.LogInformation("GenerateAssessment called. Requirements length: {Len}",
                request.Requirements.Length);

            var session = await CreateAssessmentSession(request);

            try
            {
                var readiness = await ValidateAssessmentRequest(
                    request.Requirements,
                    new List<QuestionAnswer>());

                await UpdateAssessmentSession(session, readiness);

                if (!readiness.IsReadyForAssessment)
                {
                    _logger.LogInformation("Session {Id}: needs more information.", session.Id);
                    return BuildPendingResponse(session.Id, session.Status, readiness);
                }

                _logger.LogInformation("Session {Id}: ready for assessment.", session.Id);
                return await FinalizeSession(session, request.Requirements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GenerateAssessment for session {Id}.", session.Id);
                await MarkSessionFailed(session);
                throw;
            }
        }

        public async Task<AssessmentSessionResponse> SubmitAnswers(SubmitAnswersRequest request)
        {
            _logger.LogInformation("SubmitAnswers called for session {Id}. Answers count: {Count}",
                request.SessionId, request.Answers.Count);

            var session = await _cosmosDbService.GetSessionAsync(request.SessionId)
                ?? throw new KeyNotFoundException($"Session {request.SessionId} not found.");

            if (session.Status == "Completed")
            {
                _logger.LogWarning("SubmitAnswers called on already-completed session {Id}.", session.Id);
                throw new InvalidOperationException($"Session {request.SessionId} is already completed.");
            }

            try
            {
                // Append new answers to the existing Q&A log
                var newEntries = request.Answers.Select(a => new QuestionAnswer
                {
                    Question   = a.Question,
                    Answer     = a.Answer,
                    CreatedDateTime = DateTime.UtcNow
                }).ToList();

                session.CollectedQuestionsAndAnswers.AddRange(newEntries);
                await _cosmosDbService.UpdateSessionAsync(session);

                _logger.LogInformation("Session {Id}: added {Count} answer(s). Total Q&A: {Total}",
                    session.Id, newEntries.Count, session.CollectedQuestionsAndAnswers.Count);

                // Build consolidated prompt from original request + all Q&A
                var consolidatedPrompt = await BuildConsolidatedPrompt(
                    session.OriginalRequest,
                    session.CollectedQuestionsAndAnswers);

                session.ConsolidatedPrompt = consolidatedPrompt;
                await _cosmosDbService.UpdateSessionAsync(session);

                // Re-validate with the richer prompt
                var readiness = await ValidateAssessmentRequest(
                    consolidatedPrompt,
                    session.CollectedQuestionsAndAnswers);

                await UpdateAssessmentSession(session, readiness);

                if (!readiness.IsReadyForAssessment)
                {
                    _logger.LogInformation("Session {Id}: still needs more information.", session.Id);
                    return BuildPendingResponse(session.Id, session.Status, readiness);
                }

                _logger.LogInformation("Session {Id}: ready for final assessment.", session.Id);
                return await FinalizeSession(session, consolidatedPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SubmitAnswers for session {Id}.", session.Id);
                await MarkSessionFailed(session);
                throw;
            }
        }

        public async Task UpdateAssessmentSession(AssessmentSession session, AssessmentReadinessResponse readiness)
        {
            session.CurrentQuestions = readiness.NextQuestions;
            session.Status = readiness.IsReadyForAssessment
                ? "ReadyForAssessment"
                : "NeedsMoreInformation";

            await _cosmosDbService.UpdateSessionAsync(session);
            _logger.LogInformation("Updated session {Id} → Status: {Status}",
                session.Id, session.Status);
        }

        private async Task MarkSessionFailed(AssessmentSession session)
        {
            try
            {
                session.Status = "Failed";
                await _cosmosDbService.UpdateSessionAsync(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not mark session {Id} as Failed.", session.Id);
            }
        }

        private async Task<AssessmentSessionResponse> FinalizeSession(AssessmentSession session, string promptToUse)
        {
            var finalAssessment = await GenerateFinalAssessment(promptToUse);

            // Embed the assessment inside the session document
            session.FinalAssessment = new Assessment
            {
                Requirements = session.OriginalRequest,
                ExecutiveSummary = finalAssessment.ExecutiveSummary,
                RecommendedServices = finalAssessment.RecommendedServices,
                Risks = finalAssessment.Risks,
                Tradeoffs = finalAssessment.Tradeoffs,
                Roadmap = finalAssessment.Roadmap,
                SessionId = session.Id,
                CreatedDateTime = DateTime.UtcNow
            };

            session.Status = "Completed";
            await _cosmosDbService.UpdateSessionAsync(session);

            await PersistFinalAssessment(finalAssessment, session.OriginalRequest, session.Id);

            _logger.LogInformation("Session {Id} completed successfully.", session.Id);

            return new AssessmentSessionResponse
            {
                SessionId = session.Id,
                Status = "Completed",
                IsReadyForAssessment = true,
                FinalAssessment = finalAssessment
            };
        }

        private static AssessmentSessionResponse BuildPendingResponse(string sessionId, string status,AssessmentReadinessResponse readiness)
        {
            return new AssessmentSessionResponse
            {
                SessionId = sessionId,
                Status = status,
                IsReadyForAssessment = false,
                MissingInformationAreas = readiness.MissingInformationAreas,
                NextQuestions = readiness.NextQuestions
            };
        } 

        public async Task PersistFinalAssessment(AssessmentResponse response, string originalRequest, string sessionId)
        {
            var doc = new Assessment
            {
                Requirements = originalRequest,
                ExecutiveSummary = response.ExecutiveSummary,
                RecommendedServices = response.RecommendedServices,
                Risks = response.Risks,
                Tradeoffs = response.Tradeoffs,
                Roadmap = response.Roadmap,
                SessionId = sessionId,
                CreatedDateTime = DateTime.UtcNow
            };

            await _cosmosDbService.CreateAssessmentAsync(doc);
            _logger.LogInformation("Persisted Assessment to Cosmos for session {Id}.", sessionId);
        }

        public async Task<AssessmentReadinessResponse> ValidateAssessmentRequest(string consolidatedPrompt, List<QuestionAnswer> previousQA)
        {
            _logger.LogInformation("ValidateAssessmentRequest — prompt length: {Len}",
                consolidatedPrompt.Length);

            var chatClient = BuildChatClient();

            var priorQuestionsBlock = previousQA.Count > 0
                ? "Previously asked questions (do NOT repeat these):\n" +
                  string.Join("\n", previousQA.Select((q, i) => $"  {i + 1}. {q.Question}"))
                : "No questions have been asked yet.";

            var assessmentReadinessJson = """
                {
                    "isReadyForAssessment": false,
                    "missingInformationAreas": [],
                    "nextQuestions": []
                }
                """;

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(
                    $"""
                    You are a friendly assistant helping someone plan a software system.
                    Your job is to decide whether you have enough information to produce
                    a solid architecture recommendation for them.

                    {priorQuestionsBlock}

                    Return ONLY valid JSON — no markdown, no commentary — in this exact shape:

                    {assessmentReadinessJson}

                    Rules:
                    - Be generous in your assessment. If the user has described a recognisable
                      system type (e.g. messaging app, e-commerce platform, ride-sharing service),
                      that alone is often enough to produce a solid recommendation.
                    - Set isReadyForAssessment to true unless critical information is genuinely
                      missing and would significantly change the architecture decisions.
                    - When true, missingInformationAreas and nextQuestions must be empty arrays.
                    - When false, ask a MAXIMUM of 3 questions — only the ones that would most
                      change the recommendation. Do not ask nice-to-have questions.
                    - Across the entire conversation, never ask more than 10 questions total.
                      If {previousQA.Count} questions have already been asked, be strongly biased
                      toward isReadyForAssessment = true.
                    - Do NOT repeat any previously asked question.
                    - Write every question as if you are talking to a business owner, not an engineer.
                      Avoid ALL technical terms — no mention of APIs, databases, latency, SLAs,
                      CDNs, OAuth, WebRTC, encryption protocols, or infrastructure terms.
                    - Each question must be one plain sentence a non-technical person can answer.
                    - Good example: "How many people do you expect to use this at the same time?"
                    - Bad example: "What are your peak concurrent user targets and SLA requirements?"
                    """
                ),

                ChatMessage.CreateUserMessage(consolidatedPrompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);
            var responseText = response.Value.Content[0].Text;

            _logger.LogDebug("ValidateAssessmentRequest raw response: {Text}", responseText);

            return JsonSerializer.Deserialize<AssessmentReadinessResponse>(responseText, _jsonOptions)
                ?? throw new InvalidOperationException("AI returned an invalid readiness response.");
        }

        public async Task<string> BuildConsolidatedPrompt(string originalRequest, List<QuestionAnswer> qa)
        {
            _logger.LogInformation("BuildConsolidatedPrompt — Q&A pairs: {Count}", qa.Count);

            var chatClient = BuildChatClient();

            var qaBlock = string.Join("\n", qa.Select((item, i) =>
                $"Q{i + 1}: {item.Question}\nA{i + 1}: {item.Answer}"));

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(
                    """
                    You are a requirements analyst.
                    You will be given an original system design request and a set of follow-up
                    question-and-answer pairs.

                    Your task: rewrite all of this into a single, clear, comprehensive
                    requirements paragraph that an architect can use directly.

                    Rules:
                    - Merge all information into flowing prose — no bullet points, no headers.
                    - Do not add opinions or recommendations.
                    - Do not ask further questions.
                    - Preserve every specific detail the user provided.
                    - Return only the rewritten requirements paragraph — no preamble, no labels.
                    """
                ),

                ChatMessage.CreateUserMessage(
                    $"""
                    Original request:
                    {originalRequest}

                    Follow-up Q&A:
                    {qaBlock}
                    """
                )
            };

            var response = await chatClient.CompleteChatAsync(messages);
            var consolidated  = response.Value.Content[0].Text;

            _logger.LogDebug("Consolidated prompt: {Text}", consolidated);
            return consolidated;
        }

        public async Task<AssessmentResponse> GenerateFinalAssessment(string consolidatedPrompt)
        {
            _logger.LogInformation("GenerateFinalAssessment — prompt length: {Len}",
                consolidatedPrompt.Length);

            var chatClient = BuildChatClient();

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(
                    """
                    You are a Principal Azure Cloud Architect producing a system design assessment.

                    Audience: non-technical business stakeholders. Use plain language.
                    Avoid jargon. When you must name a technology, add a one-sentence plain-
                    English description of what it does.

                    Guidance:
                    - Prefer current, generally available Microsoft Azure services.
                    - Do NOT recommend retired, deprecated, preview-only, or end-of-life services.
                    - If no strong Azure equivalent exists, recommend the best available alternative.
                    - RecommendedServices: format each entry as
                        "Service Name — what it does and why it fits here."
                    - Risks: frame each as a business impact, not a technical warning.
                    - Tradeoffs: explain what the organisation gains and what it gives up.
                    - Roadmap: 4–6 items, each a single complete action sentence in logical order.
                    - ExecutiveSummary: 3–5 sentences summarising the overall recommendation.

                    Return ONLY valid JSON — no markdown, no extra fields — matching this shape exactly:
                    {
                        "executiveSummary": "...",
                        "recommendedServices": ["...", "..."],
                        "risks": ["...", "..."],
                        "tradeoffs": ["...", "..."],
                        "roadmap": ["...", "...", "...", "...", "..."]
                    }
                    """
                ),

                ChatMessage.CreateUserMessage(consolidatedPrompt)
            };

            var response     = await chatClient.CompleteChatAsync(messages);
            var responseText = response.Value.Content[0].Text;

            _logger.LogDebug("GenerateFinalAssessment raw response: {Text}", responseText);

            return JsonSerializer.Deserialize<AssessmentResponse>(responseText, _jsonOptions)
                ?? throw new InvalidOperationException("AI returned an invalid assessment response.");
        }

        public async Task<AssessmentSession> GetSessionById(string id)
        {
            return await _cosmosDbService.GetSessionAsync(id)
                ?? throw new KeyNotFoundException($"Session {id} not found.");
        }

        public async Task<List<SessionSummaryResponse>> GetAllSessions()
        {
            var sessions = await _cosmosDbService.GetAllSessionsAsync();

            return sessions.Select(s => new SessionSummaryResponse
            {
                Id = s.Id,
                OriginalRequest = s.OriginalRequest,
                Status = s.Status,
                CreatedDateTime = s.CreatedDateTime
            }).ToList();
        }

        private ChatClient BuildChatClient()
        {
            var azureClient = new AzureOpenAIClient(
                new Uri(_options.Endpoint),
                new AzureKeyCredential(_options.ApiKey));

            return azureClient.GetChatClient(_options.DeploymentName);
        }
    }
}