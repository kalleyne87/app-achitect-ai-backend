# ArchitectAI — Backend API

> An intelligent, multi-turn system design assessment platform that guides users from an app idea to a full architecture recommendation. Defaults to Azure but recommends whatever platform best fits the user's needs. Powered by Azure OpenAI (GPT-4o) and built on a .NET 10 backend.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Azure OpenAI](https://img.shields.io/badge/Azure%20OpenAI-GPT--4o-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
[![Cosmos DB](https://img.shields.io/badge/Azure%20Cosmos%20DB-NoSQL-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/cosmos-db)

---

## What It Does

ArchitectAI takes a user's app idea and, through a guided multi-turn AI conversation, produces a complete architectural assessment. Rather than returning a generic answer to a vague prompt, the system validates whether it has enough context before generating a recommendation. If not, it asks plain-language follow-up questions until the information is sufficient — no technical knowledge required from the user.

Recommendations default to Azure, but the system will suggest other technologies and platforms when they're the better fit.

**The output includes:**
- Executive summary
- Recommended services and technologies
- Architectural tradeoffs
- Risk analysis
- Implementation roadmap

**Frontend repo:** [app-architect-ai-ui](https://github.com/kalleyne87/app-architect-ai-ui)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Angular 22 UI                            │
│              NgRx Signal Store · Standalone Components          │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTP (REST) + X-Api-Key header
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                   ArchitectAI.Api (.NET 10)                     │
│         ASP.NET Core Web API · Rate Limiting · API Key Auth     │
└──────┬──────────────────────────────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────────────────┐
│                   ArchitectAI.Business                          │
│                                                                 │
│  AssessmentService                                              │
│  ├── ValidateAssessmentRequest()   — AI readiness check         │
│  ├── BuildConsolidatedPrompt()     — merge original + Q&A       │
│  ├── GenerateFinalAssessment()     — produce recommendation     │
│  └── PersistFinalAssessment()      — save to Cosmos DB          │
│                                                                 │
│  CosmosDbService                                                │
│  ├── Sessions container            — full conversation state    │
│  └── Assessments container         — completed results          │
└──────┬──────────────────────────────────────────────────────────┘
       │                              │
       ▼                              ▼
┌─────────────────────┐   ┌──────────────────────────────────────┐
│   Azure Cosmos DB   │   │          Azure OpenAI (GPT-4o)       │
│                     │   │                                      │
│  Sessions           │   │  • Validate request readiness        │
│  Assessments        │   │  • Generate follow-up questions      │
└─────────────────────┘   │  • Build consolidated prompt         │
                          │  • Generate final assessment         │
                          └──────────────────────────────────────┘
```

---

## Assessment Flow

```
User submits app idea
        │
        ▼
CreateAssessmentSession() — persisted to Cosmos immediately
        │
        ▼
ValidateAssessmentRequest()
        │
        ├── Not enough info? ──► Return probing questions (max 3 per round)
        │                                   │
        │                        User submits answers
        │                                   │
        │                        SubmitAnswers()
        │                                   │
        │                        Append Q&A to session
        │                                   │
        │                        BuildConsolidatedPrompt()
        │                                   │
        │                        ValidateAssessmentRequest() ◄── loop until ready
        │
        └── Enough info? ──►
                │
                ▼
        GenerateFinalAssessment()
                │
                ▼
        PersistFinalAssessment()   — saved to Assessments + embedded in Session
                │
                ▼
        Return AssessmentSessionResponse
          • Executive Summary
          • Recommended Services
          • Tradeoffs
          • Risks
          • Roadmap
```

---

## Data Model

### AssessmentSession
Tracks the full lifecycle of a user's conversation with the AI.

| Field | Type | Description |
|---|---|---|
| `id` | string (GUID) | Cosmos partition key |
| `originalRequest` | string | The user's initial app idea |
| `status` | string | `NeedsMoreInformation` · `ReadyForAssessment` · `Completed` · `Failed` |
| `currentQuestions` | string[] | Questions currently awaiting user answers |
| `collectedQuestionsAndAnswers` | object[] | Full Q&A history for the session |
| `consolidatedPrompt` | string | Merged prompt sent to OpenAI for final generation |
| `finalAssessment` | object | Embedded assessment result (also persisted separately) |
| `createdDateTime` | DateTime | Session creation timestamp |
| `updatedDateTime` | DateTime | Last update timestamp |

### Assessment
The final persisted output returned to the user.

| Field | Type | Description |
|---|---|---|
| `id` | string (GUID) | Cosmos partition key |
| `requirements` | string | Original user request |
| `executiveSummary` | string | High-level summary of the recommendation |
| `recommendedServices` | string[] | Recommended services with plain-English descriptions |
| `tradeoffs` | string[] | Architectural tradeoffs |
| `risks` | string[] | Business-framed risks |
| `roadmap` | string[] | Step-by-step implementation plan |
| `sessionId` | string | Reference back to the originating session |
| `createdDateTime` | DateTime | Assessment creation timestamp |

---

## Solution Structure
```
app-architect-ai-backend/
├── ArchitectAI.Api/
│   ├── Controllers/
│   │   └── AssessmentsController.cs   # All REST endpoints
│   └── Program.cs                     # DI, middleware, rate limiting, API key auth
│
├── ArchitectAI.Business/
│   ├── Mappers/
│   │   └── AssessmentProfile.cs       # AutoMapper DBO ↔ DTO
│   ├── Options/
│   │   ├── AzureOpenAIOptions.cs
│   │   └── CosmosDbOptions.cs
│   └── Services/
│       ├── Interface/
│       │   ├── IAssessmentService.cs
│       │   └── ICosmosDbService.cs
│       ├── AssessmentService.cs       # Core business + AI logic
│       └── CosmosDbService.cs         # Cosmos DB read/write
│
└── ArchitectAI.DomainObjects/
    ├── DBOs/
    │   └── CosmosDocuments.cs         # AssessmentSessionDocument, AssessmentDocument
    └── DTOs/
        ├── AssessmentRequest.cs
        ├── AssessmentResponse.cs
        ├── AssessmentSessionResponse.cs
        ├── AssessmentReadinessResponse.cs
        ├── SubmitAnswersRequest.cs
        └── SessionSummaryResponse.cs
```

---

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/assessments` | All completed assessments |
| `GET` | `/api/assessments/{id}` | Single assessment by ID |
| `POST` | `/api/assessments` | Start a new assessment session |
| `POST` | `/api/assessments/answers` | Submit answers to follow-up questions |
| `GET` | `/api/assessments/sessions` | All sessions (for sidebar history) |
| `GET` | `/api/assessments/sessions/{id}` | Full session with Q&A transcript |

All endpoints require the `X-Api-Key` header.

---

## Security

**API key authentication** — all `/api/assessments` routes require an `X-Api-Key` header matching the value in `appsettings.json`. This prevents unauthorised use of the Azure OpenAI endpoint.

**Rate limiting** — powered by `AspNetCoreRateLimit`:
- `POST /api/assessments` — 5 requests per minute per IP
- `POST /api/assessments/answers` — 10 requests per minute per IP

**Azure spending cap** — set a monthly budget alert in Azure Cost Management and a token quota on your OpenAI deployment as a hard backstop.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API |
| AI | Azure OpenAI (GPT-4o) |
| Database | Azure Cosmos DB for NoSQL (Serverless) |
| Object Mapping | AutoMapper |
| Rate Limiting | AspNetCoreRateLimit |

---

## Local Development

### Prerequisites
- .NET 10 SDK
- Azure OpenAI resource with a GPT-4o deployment
- Azure Cosmos DB account (free serverless tier works)

### Configuration

Copy `appsettings.example.json` to `appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "ApiKey": "<your-key>",
    "DeploymentName": "gpt-4o"
  },
  "CosmosDb": {
    "Endpoint": "https://<your-account>.documents.azure.com:443/",
    "Key": "<your-primary-key>",
    "DatabaseName": "AppArchitectAIDb",
    "SessionsContainer": "Sessions",
    "AssessmentsContainer": "Assessments"
  },
  "ApiSettings": {
    "AppArchitectAIKey2026": "<your-shared-secret>"
  }
}
```

### Run Locally

```bash
git clone https://github.com/kalleyne87/app-architect-ai-backend.git
cd app-architect-ai-backend
dotnet restore
dotnet run --project ArchitectAI.Api
```

API runs at `http://localhost:5197`.  
Swagger UI available at `http://localhost:5197/swagger` in development.

---

## Roadmap

- [x] Multi-turn AI conversation with readiness validation
- [x] Plain-language follow-up questions (non-technical user friendly)
- [x] Session persistence in Azure Cosmos DB
- [x] Final assessment generation and storage
- [x] Full session history and transcript retrieval
- [x] API key protection
- [x] IP-based rate limiting
- [ ] Azure Service Bus integration for async processing
- [ ] Azure Functions queue consumer
- [ ] Azure App Service deployment
- [ ] Per-user session scoping

---

## About

Built as a portfolio project to demonstrate Staff/Architect-level distributed systems design. The architecture explores real-world patterns including multi-turn AI context management, NoSQL document modelling, layered .NET solution structure, and a clean API contract, rather than a simple prompt/response AI wrapper.

**Frontend:** [app-architect-ai-ui](https://github.com/kalleyne87/app-architect-ai-ui) — Angular 22 · NgRx Signal Store · Standalone Components