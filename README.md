# Recommendation System for Learning (RSL)

**Recommendation System for Learning (RSL)** is a personalized recommendation system designed to suggest relevant learning resources.
Although it is primarily intended for personal use (a single user), it is architected as a **multi-user, production-style system** to practice good design patterns and scalability.

> **Status:** Work in progress – early design and foundation phase.
> The codebase, architecture, and feature set are evolving and subject to change.

## Vision

RSL aims to:

- Help users **log what they study** (papers, videos, blog posts, etc.).
- **Recommend** what to learn next based on history, preferences, and resource metadata.
- Provide a **daily “1 paper, 1 video, 1 blog post” digest** via email.
- Experiment with:
  - A traditional ML-based recommendation engine (ML.NET).
  - An **LLM “agentic” layer** that can interpret, refine, and explain recommendations.

## High-Level Architecture

At a high level, RSL is composed of:

### 🎨 Blazor Frontend
- Rich UI for interacting with the system.
- User flows for logging study activity, viewing recommendations, and managing preferences.

### 🔧 .NET Backend + REST API
- Central application layer (business logic, validation, orchestration).
- Multi-user–ready endpoints for users, resources, study logs, and recommendations.

### 🤖 Recommendation Engine (ML.NET)
- Core recommendations based on user–resource interactions (e.g., matrix factorization).
- Extensible to combine simple rule-based logic with ML-based scoring.

### 🧠 LLM Orchestration Layer
- LLM "agent" that can:
  - Request candidate recommendations from the engine.
  - Refine them based on user constraints (time, difficulty, topics).
  - Generate textual explanations and study plans.
- Exposed through the backend as a service/API.

### ⏰ Background Jobs & Scheduling
- Daily job to generate and send the "1 paper, 1 video, 1 blog post" email.
- Periodic retraining or refreshing of ML models.

### ☁️ Infrastructure (Azure)
- Application hosting, database, storage, observability, and email integration.

## Solution / Project Layout

The solution is organized as multiple projects following a modular, layered approach:

```text
recommendation-system-for-learning/
├─ src/
│  ├─ Rsl.Web/              # Blazor frontend (UI, pages, components)
│  ├─ Rsl.Api/              # ASP.NET Core REST API (HTTP endpoints)
│  ├─ Rsl.Core/             # Domain models, interfaces, core business rules
│  ├─ Rsl.Recommendation/   # ML.NET-based recommendation logic
│  ├─ Rsl.Llm/              # LLM / “agentic” orchestration and tool interfaces
│  ├─ Rsl.Infrastructure/   # Persistence, logging, monitoring, email, external integrations, Azure services
│  └─ Rsl.Jobs/             # Background jobs (e.g., daily digest, retraining)
│
├─ tests/
│  └─ Rsl.Tests/            # Unit and integration tests
```
