using System.Text.Json;
using AutoMapper;
using Azure;
using Azure.AI.OpenAI;
using AppAssessment.Business.Data;
using AppAssessment.Business.Services.Interface;
using AppAssessment.DomainObjects.DBOs;
using AppAssessment.DomainObjects.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Microsoft.Extensions.Logging;


namespace AppAssessment.Business.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly AzureOpenAIOptions _options;
        private readonly SqlDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<AssessmentService> _logger;

        public AssessmentService(IOptions<AzureOpenAIOptions> options, SqlDbContext dbContext, IMapper mapper, ILogger<AssessmentService> logger)
        {
            _options = options.Value;
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        private static readonly JsonSerializerOptions _jsonOptions =
            new() { PropertyNameCaseInsensitive = true };

        public async Task<List<AssessmentResponse>> GetAssessments()
        {
            try
            {
                var dbAssessments = await _dbContext.Assessments
                    .OrderByDescending(x => x.CreatedDateTime)
                    .ToListAsync();
 
                return _mapper.Map<List<AssessmentResponse>>(dbAssessments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all assessments.");
                throw;
            }
        }

        public async Task<AssessmentResponse> GetAssessmentById(int id)
        {
            try
            {
                var assessment = await _dbContext.Assessments.FindAsync(id)
                    ?? throw new KeyNotFoundException($"Assessment with ID {id} not found.");
 
                return _mapper.Map<AssessmentResponse>(assessment);
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
                CollectedQuestionsAndAnswersJson = "[]",
                CurrentQuestionsJson = "[]",
                Status = "NeedsMoreInformation",
                CreatedDateTime = DateTime.UtcNow,
                UpdatedDateTime = DateTime.UtcNow
            };
 
            _dbContext.AssessmentSessions.Add(session);
            await _dbContext.SaveChangesAsync();
 
            _logger.LogInformation("Created AssessmentSession {Id}.", session.Id);
            return session;
        }

        public async Task<AssessmentSessionResponse> GenerateAssessment(AssessmentRequest request)
        {
            _logger.LogInformation("GenerateAssessment called. Requirements length: {Len}", request.Requirements.Length);

            // Create a new assessment session
            var session = await CreateAssessmentSession(request);

            try
            {
                // Validate the request to see if more information is needed
                var readiness = await ValidateAssessmentRequest(request.Requirements, new List<QuestionAnswer>());
 
                // Update the session with the AI's response
                await UpdateAssessmentSession(session, readiness);
 
                // Check if enough information is available
                if (!readiness.IsReadyForAssessment)
                {
                    _logger.LogInformation("Session {Id}: needs more information.", session.Id);
                    return BuildPendingResponse(session.Id, session.Status, readiness);
                }
 
                // 4b. Enough info — generate and return the full assessment
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
 
            // 1. Load existing session
            var session = await _dbContext.AssessmentSessions.FindAsync(request.SessionId)
                ?? throw new KeyNotFoundException($"Session {request.SessionId} not found.");
 
            if (session.Status == "Completed")
            {
                _logger.LogWarning("SubmitAnswers called on already-completed session {Id}.", session.Id);
                throw new InvalidOperationException($"Session {request.SessionId} is already completed.");
            }
 
            try
            {
                // Deserialise the existing Q&A log 
                var existingQA = DeserializeQA(session.CollectedQuestionsAndAnswersJson);
 
                var newEntries = request.Answers.Select(a => new QuestionAnswer
                {
                    Question   = a.Question,
                    Answer     = a.Answer,
                    CreatedDateTime = DateTime.UtcNow
                }).ToList();
 
                existingQA.AddRange(newEntries);
 
                // Persist the updated Q&A immediately (audit trail)
                session.CollectedQuestionsAndAnswersJson = JsonSerializer.Serialize(existingQA);
                session.UpdatedDateTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
 
                _logger.LogInformation("Session {Id}: appended {Count} answer(s). Total Q&A: {Total}",
                    session.Id, newEntries.Count, existingQA.Count);
 
                // Build a consolidated prompt that merges original request
                var consolidatedPrompt = await BuildConsolidatedPrompt(session.OriginalRequest, existingQA);
 
                // Persist the consolidated prompt for debugging / future reference
                session.ConsolidatedPrompt = consolidatedPrompt;
                session.UpdatedDateTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
 
                // Re-validate request with more informed
                var readiness = await ValidateAssessmentRequest(consolidatedPrompt, existingQA);
                await UpdateAssessmentSession(session, readiness);
 
                // Check if still ready or not 
                // Return the next round of questions if not ready
                if (!readiness.IsReadyForAssessment)
                {
                    _logger.LogInformation("Session {Id}: still needs more information after answers.", session.Id);
                    return BuildPendingResponse(session.Id, session.Status, readiness);
                }
 
                // Produce the final assessment 
                _logger.LogInformation("Session {Id}: ready for final assessment after answers.", session.Id);
                return await FinalizeSession(session, consolidatedPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SubmitAnswers for session {Id}.", session.Id);
                await MarkSessionFailed(session);
                throw;
            }
        }

        public async Task<bool> UpdateAssessmentSession(AssessmentSession sessionCreated, AssessmentReadinessResponse readiness)
        {
            var tracked = await _dbContext.AssessmentSessions.FindAsync(sessionCreated.Id)
                ?? throw new InvalidOperationException($"Session {sessionCreated.Id} not found during update.");
 
            tracked.CurrentQuestionsJson = JsonSerializer.Serialize(readiness.NextQuestions);
            tracked.Status               = readiness.IsReadyForAssessment
                ? "ReadyForAssessment"
                : "NeedsMoreInformation";
            tracked.UpdatedDateTime = DateTime.UtcNow;
 
            var result = await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Updated session {Id} → Status: {Status}", sessionCreated.Id, tracked.Status);
            return result > 0;
        }
        
        public async Task<AssessmentReadinessResponse> ValidateAssessmentRequest(string consolidatedPrompt, List<QuestionAnswer> previousQA)
        {
            _logger.LogInformation("ValidateAssessmentRequest — prompt length: {Len}", consolidatedPrompt.Length);
 
            var chatClient = BuildChatClient();
 
            // Summarise previously asked questions to avoid repeats
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
                    You are a Principal Azure Cloud Architect conducting a requirements review.
                    Your job is to decide whether the information provided is detailed enough to
                    produce an accurate, Azure-focused architecture recommendation.
 
                    {priorQuestionsBlock}
 
                    Return ONLY valid JSON — no markdown, no commentary — in this exact shape:
                    
                    {assessmentReadinessJson}
 
                    Rules:
                    - Set isReadyForAssessment to true only when you are confident you can
                      produce a meaningful recommendation without guessing on major points.
                    - When true, missingInformationAreas and nextQuestions must be empty arrays.
                    - When false, ask 3–5 NEW, specific follow-up questions that materially
                      affect the architecture (e.g. scale, compliance, budget, existing systems).
                    - Do NOT repeat any previously asked question.
                    - Write questions in plain, friendly language a non-technical person understands.
                    """
                ),
 
                ChatMessage.CreateUserMessage(consolidatedPrompt)
            };
            
            var response     = await chatClient.CompleteChatAsync(messages);
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
            var consolidated = response.Value.Content[0].Text;
 
            _logger.LogDebug("Consolidated prompt: {Text}", consolidated);
            return consolidated;
        }

        public async Task<AssessmentResponse> GenerateFinalAssessment(string consolidatedPrompt)
        {
            _logger.LogInformation("GenerateFinalAssessment — prompt length: {Len}", consolidatedPrompt.Length);
 
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

        public async Task<bool> PersistFinalAssessment(AssessmentResponse response, string originalRequest)
        {
            var entity = _mapper.Map<Assessment>(response);
            entity.Requirements = originalRequest;
            entity.CreatedDateTime = DateTime.UtcNow;
 
            _dbContext.Assessments.Add(entity);
            var result = await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Persisted Assessment record (Requirements length: {Len}).", originalRequest.Length);
            return result > 0;
        }

        private async Task MarkSessionFailed(AssessmentSession session)
        {
            try
            {
                var tracked = await _dbContext.AssessmentSessions.FindAsync(session.Id);
                if (tracked is null) return;
 
                tracked.Status    = "Failed";
                tracked.UpdatedDateTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not mark session {Id} as Failed.", session.Id);
            }
        }

        private async Task<AssessmentSessionResponse> FinalizeSession(AssessmentSession session, string promptToUse)
        {
            var finalAssessment = await GenerateFinalAssessment(promptToUse);
 
            // Update the session row
            var tracked = await _dbContext.AssessmentSessions.FindAsync(session.Id)
                ?? throw new InvalidOperationException($"Session {session.Id} disappeared before finalisation.");
 
            tracked.FinalAssessmentJson = JsonSerializer.Serialize(finalAssessment);
            tracked.Status = "Completed";
            tracked.UpdatedDateTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
 
            // Add a separate long-term Assessment record for reporting/history
            await PersistFinalAssessment(finalAssessment, session.OriginalRequest);
 
            _logger.LogInformation("Session {Id} completed successfully.", session.Id);
 
            return new AssessmentSessionResponse
            {
                SessionId = session.Id,
                Status = "Completed",
                IsReadyForAssessment = true,
                FinalAssessment = finalAssessment
            };
        }

        private static AssessmentSessionResponse BuildPendingResponse(int sessionId, string status, AssessmentReadinessResponse readiness)
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

        private ChatClient BuildChatClient()
        {
            var azureClient = new AzureOpenAIClient(
                new Uri(_options.Endpoint),
                new AzureKeyCredential(_options.ApiKey));
 
            return azureClient.GetChatClient(_options.DeploymentName);
        }

        private static List<QuestionAnswer> DeserializeQA(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
                return new List<QuestionAnswer>();
 
            return JsonSerializer.Deserialize<List<QuestionAnswer>>(json, _jsonOptions)
                ?? new List<QuestionAnswer>();
        }
    }
}
