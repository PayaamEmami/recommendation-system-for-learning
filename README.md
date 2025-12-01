# Recommendation System for Learning (RSL)

**Recommendation System for Learning (RSL)** is a personalized recommendation system designed to aggregate and suggest relevant learning resources from user-defined sources.
Although it is primarily intended for personal use (a single user), it is architected as a **multi-user, production-style system** to practice good design patterns and scalability.

> **Status:** Work in progress – early design and foundation phase.
> The codebase, architecture, and feature set are evolving and subject to change.

## Vision

RSL aims to:

- Help users **add and manage URL-based sources** (RSS feeds, YouTube channels, blogs, newsletters, etc.) organized by content category.
- **Automatically ingest and aggregate** learning resources from these sources.
- Help users **log what they've studied** and track their learning activity.
- Provide **personalized feeds** in the web app for different content types:
  - Papers
  - Videos
  - Blog posts
  - Current Events
  - Social Media Posts
- Experiment with:
  - A traditional ML-based recommendation engine (ML.NET).
  - An **LLM "agentic" layer** that can interpret, refine, and explain recommendations.

## High-Level Architecture

At a high level, RSL is composed of:

### 🎨 Blazor Frontend
- Multiple feed types (Papers, Videos, Blog Posts, Current Events, Social Media Posts).
- User flows for logging study activity, browsing personalized feeds, and managing preferences.

### 🔧 .NET Backend + REST API
- Central application layer (business logic, validation, orchestration).
- Multi-user–ready endpoints for:
  - Users (authentication, profiles)
  - Sources (URL-based content sources)
  - Resources (aggregated content items)
  - Study logs and votes
  - Recommendations

### 📡 Data Ingestion Layer
- Pulls content from user-configured sources:
  - RSS/Atom feeds (blogs, papers, news)
  - YouTube channel/playlist APIs
  - Newsletter integrations
  - Social media APIs (future)
- Parses and normalizes content into Resource entities
- Associates resources with their originating Source

### 🤖 Recommendation Engine
- Simple, practical recommendation logic based on:
  - Recency (prioritize newer content)
  - Source preferences (from user's configured sources)
  - User feedback (upvotes/downvotes)

### 🧠 LLM Orchestration Layer
- LLM "agent" that can:
  - Request candidate recommendations from the engine.
  - Refine them based on user constraints (time, difficulty, topics).
  - Generate textual explanations and study plans.
- Exposed through the backend as a service/API.

### ⏰ Background Jobs & Scheduling
- Periodic jobs to refresh and populate recommendation feeds.
- Periodic retraining or refreshing of ML models.

### ☁️ Infrastructure (Azure)
- Application hosting, database, storage, observability, and notification services.

## Solution / Project Layout

The solution is organized as multiple projects following a modular, layered approach:

```text
recommendation-system-for-learning/
├─ src/
│  ├─ Rsl.Api/              # ASP.NET Core REST API (HTTP endpoints)
│  ├─ Rsl.Core/             # Domain models, interfaces, core business rules
│  ├─ Rsl.Infrastructure/   # Persistence, logging, monitoring, email, external integrations, Azure services
│  ├─ Rsl.Jobs/             # Background jobs (e.g., daily digest, retraining)
│  ├─ Rsl.Recommendation/   # ML.NET-based recommendation logic
│  ├─ Rsl.Llm/              # LLM / “agentic” orchestration and tool interfaces
│  └─ Rsl.Web/              # Blazor frontend (UI, pages, components)
│
├─ tests/
│  └─ Rsl.Tests/            # Unit and integration tests
```
