# ArchitectAI — Backend API

> An intelligent, multi-turn application assessment platform that guides users from an app idea to a full architecture recommendation that defaults to Azure recommendations, but will suggest other technologies and platforms when they're the better fit. Powered by Azure OpenAI and built on a distributed .NET backend.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Azure OpenAI](https://img.shields.io/badge/Azure%20OpenAI-GPT--4o-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
[![Cosmos DB](https://img.shields.io/badge/Azure%20Cosmos%20DB-NoSQL-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/cosmos-db)
[![Service Bus](https://img.shields.io/badge/Azure%20Service%20Bus-Messaging-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/service-bus)
[![App Service](https://img.shields.io/badge/Azure%20App%20Service-Hosted-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/app-service)

---

## What It Does

ArchitectAI takes a user's app idea and, through a guided, multi-turn AI conversation it produces a complete architectural assessment. Rather than returning a generic answer to a vague prompt, the system validates whether it has enough context before generating a recommendation. If not, it asks targeted follow-up questions until the information is sufficient.

Recommendations default to Azure, but the system will suggest other technologies and platforms when they're the better fit for the user's needs.

**The output includes:**
- Executive summary
- Recommended services and technologies
- System requirements
- Architectural tradeoffs
- Risk analysis
- Implementation roadmap

**Frontend repo:** [app-assessment-ui](https://github.com/kalleyne87/app-assessment-ui)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Angular 22 UI                            │
│                    (app-assessment-ui)                          │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTP (REST)
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                   ArchitectAI.Api (.NET 10)                   │
│              Azure App Service — REST Controllers               │
└──────┬──────────────────────────────────────────┬──────────────┘
       │ Service Layer                            │ Enqueue Request
       ▼                                          ▼
┌─────────────────────┐              ┌────────────────────────────┐
│ ArchitectAI       │              │    Azure Service Bus        │
│ .Business           │              │    (Assessment Queue)       │
│                     │              └────────────┬───────────────┘
│ • AssessmentService │                           │ Consume
│ • Validation Logic  │              ┌────────────▼───────────────┐
│ • Prompt Builder    │              │    Azure Functions          │
│ • AutoMapper        │              │    (Queue Consumer)         │
└──────┬──────────────┘              └────────────┬───────────────┘
       │                                          │
       ▼                                          ▼
┌─────────────────────┐              ┌────────────────────────────┐
│   Azure Cosmos DB   │              │     Azure OpenAI            │
│   (NoSQL)           │◄─────────────│     (GPT-4o)               │
│                     │   Persist    │                             │
│ • AssessmentSession │              │ • Validate readiness        │
│ • Assessment        │              │ • Generate questions        │
└─────────────────────┘              │ • Build final assessment    │
                                     └────────────────────────────┘
```

### Key Architectural Decisions

**Multi-turn validation before generation**
Rather than generating an assessment from a single prompt, the system calls Azure OpenAI to validate whether the user's input is sufficient. If not, it returns targeted questions. This loop continues until readiness is confirmed, at which point the consolidated prompt is passed to a final generation call. This avoids low-quality output from vague inputs.

**Asynchronous processing via Azure Service Bus**
Assessment generation is compute-heavy and involves multiple OpenAI calls. Rather than blocking the HTTP response, the API enqueues the request to an Azure Service Bus queue and returns immediately. An Azure Function consumes the queue message and handles the full generation pipeline and keeping the API responsive and the processing decoupled.

**Cosmos DB for session persistence**
The multi-turn conversation state (`AssessmentSession`) — including the original request, all collected Q&A, status, and the consolidated prompt is persisted in Azure Cosmos DB. This allows sessions to survive across requests and supports future features like session resumption and history retrieval.

**Layered .NET solution with clean separation**
The solution follows a three-project structure: `Api` (controllers, routing), `Business` (services, data access, AutoMapper, EF migrations), and `DomainObjects` (DTOs and DBOs). This keeps the domain model independent of infrastructure concerns and makes each layer independently testable.

---

## Solution Structure

```
app-assessment-backend/
├── ArchitectAI.Api/              # Controllers, middleware, DI registration
├── ArchitectAI.Business/
│   ├── Data/                       # DbContext, Cosmos DB configuration
│   ├── Mappers/                    # AutoMapper profiles (DBO ↔ DTO)
│   ├── Migrations/                 # EF Core schema migrations
│   └── Services/
│       ├── Interface/              # IAssessmentService contract
│       └── AssessmentService.cs    # Core business logic
└── ArchitectAI.DomainObjects/
    ├── DBOs/                       # Database objects (AssessmentSession, Assessment)
    └── DTOs/                       # API request/response contracts
```

---

## Assessment Flow

```
User submits app idea
        │
        ▼
CreateAssessmentSession()
        │
        ▼
ValidateAssessmentRequest()  ──── Insufficient? ──► Return questions to user
        │                                                      │
    Sufficient?                                     User submits answers
        │                                                      │
        ▼                                           SubmitAnswers()
BuildConsolidatedPrompt()  ◄───────────────────────────────────┘
        │
        ▼
GenerateFinalAssessment()
        │
        ▼
PersistFinalAssessment()
        │
        ▼
Return AssessmentResponse
  • Executive Summary
  • Recommended Services
  • Requirements
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
| `Id` | int | Primary key |
| `OriginalRequest` | string | The user's initial app idea |
| `Status` | string | `NeedsMoreInformation` \| `ReadyForAssessment` \| `Complete` |
| `CurrentQuestionsJson` | JSON | Questions currently awaiting user answers |
| `CollectedQuestionsAndAnswersJson` | JSON | Full Q&A history for the session |
| `ConsolidatedPrompt` | string | The final enriched prompt sent to OpenAI |
| `FinalAssessmentJson` | JSON | The persisted assessment result |
| `CreatedDateTime` | DateTime | Session creation timestamp |
| `UpdatedDateTime` | DateTime? | Last update timestamp |

### Assessment
The final persisted output returned to the user.

| Field | Type | Description |
|---|---|---|
| `Id` | int | Primary key |
| `ExecutiveSummary` | string | High-level summary of the recommendation |
| `Requirements` | string | Identified system requirements |
| `RecommendedServicesJson` | JSON | List of recommended Azure/tech services |
| `TradeoffsJson` | JSON | Architectural tradeoffs |
| `RisksJson` | JSON | Identified risks |
| `RoadmapJson` | JSON | Implementation roadmap steps |
| `CreatedDateTime` | DateTime | Assessment creation timestamp |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API |
| AI | Azure OpenAI (GPT-4o) |
| Database | Azure Cosmos DB (NoSQL) |
| Messaging | Azure Service Bus |
| Background Processing | Azure Functions |
| ORM / Migrations | Entity Framework Core |
| Object Mapping | AutoMapper |
| Hosting | Azure App Service |

---

## Local Development

### Prerequisites
- .NET 10 SDK
- Azure subscription (or [Azure free account](https://azure.microsoft.com/free/))
- Azure OpenAI resource with a GPT-4o deployment
- Azure Cosmos DB account (free tier available)
- Azure Service Bus namespace (Basic tier)

### Configuration

Copy `appsettings.example.json` to `appsettings.Development.json` and fill in your values:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "ApiKey": "<your-key>",
    "DeploymentName": "gpt-4o"
  },
  "CosmosDB": {
    "ConnectionString": "<your-connection-string>",
    "DatabaseName": "ArchitectAI",
    "ContainerName": "Assessments"
  },
  "ServiceBus": {
    "ConnectionString": "<your-connection-string>",
    "QueueName": "assessment-queue"
  }
}
```

### Run Locally

```bash
git clone https://github.com/kalleyne87/app-assessment-backend.git
cd app-assessment-backend
dotnet restore
dotnet ef database update --project ArchitectAI.Business
dotnet run --project ArchitectAI.Api
```

API will be available at `https://localhost:5001`.

---

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/assessments` | Retrieve all completed assessments |
| `GET` | `/api/assessments/{id}` | Retrieve a specific assessment |
| `POST` | `/api/assessments` | Create a new assessment session |
| `POST` | `/api/assessments/answer` | Submit answers to follow-up questions |

---

## Roadmap

- [x] Multi-turn AI conversation flow
- [x] Assessment session persistence
- [x] Final assessment generation and storage
- [x] AutoMapper DTO/DBO pipeline
- [ ] Migrate from SQLite to Azure Cosmos DB
- [ ] Azure Service Bus integration for async processing
- [ ] Azure Functions queue consumer
- [ ] Azure App Service deployment
- [ ] API key protection for OpenAI endpoint
- [ ] Session history and retrieval by user

---

## About

Built as a portfolio project to demonstrate Staff-level distributed systems design. The architecture intentionally explores real-world patterns, async messaging, multi-turn AI context management, and a clean layered .NET API rather than a simple request/response AI wrapper. Defaults to Azure recommendations but suggests whatever platform and technology best fits the user's needs.

**Related:** [app-assessment-ui](https://github.com/kalleyne87/app-assessment-ui) — Angular 19 frontend with NgRx Signal Store
