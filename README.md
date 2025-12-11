# Recommendation System for Learning (RSL)

**Recommendation System for Learning (RSL)** is a personalized recommendation system designed to aggregate and suggest relevant learning resources from user-defined sources.

> **Status:** Work in progress – early design and foundation phase.
> The codebase, architecture, and feature set are evolving and subject to change.

## Vision

RSL aims to:

- **Add and manage URL-based sources** (RSS feeds, YouTube channels, blogs, newsletters, etc.) organized by content category.
- **Automatically ingest and aggregate** learning resources from these sources.
- **Log what you've studied** and track learning activity.
- Provide **personalized feeds** for different content types:
  - Papers
  - Videos
  - Blog posts
  - Current Events
  - Social Media Posts
- Experiment with:
  - A **hybrid recommendation engine** combining vector embeddings with traditional signals.
  - An **LLM "agentic" layer** for content ingestion and future recommendation refinement.

## High-Level Architecture

At a high level, RSL is composed of:

### 🎨 Blazor Frontend
- Multiple feed types (Papers, Videos, Blog Posts, Current Events, Social Media Posts).
- User flows for browsing personalized feeds and managing preferences.

### 🔧 .NET Backend + REST API
- Central application layer (business logic, validation, orchestration).
- REST endpoints for:
  - User profile and authentication
  - Source management (URL-based content sources)
  - Resource aggregation (content items)
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
- **Hybrid recommendation system** combining:
  - **Vector similarity search** using text embeddings (primary signal, 70% weight)
    - Resources embedded using Azure OpenAI embeddings
    - Preferences represented as aggregated embeddings of upvoted content
    - Semantic similarity matching via Azure AI Search vector database
  - **Heuristic signals** (secondary signals, 30% weight)
    - Recency (exponential decay favoring newer content)
    - Source preferences (from configured sources and voting history)
    - Feedback patterns (upvotes/downvotes)
- Filters for diversity, deduplication, and personalization

### 🧠 LLM Orchestration Layer
- **Ingestion Agent**: LLM-powered content extraction from any URL
  - Automatically categorizes resources (Papers, Videos, Blog Posts, etc.)
  - Extracts metadata and handles duplicate detection
  - Flexible, no custom parsers needed per source
- **Future**: LLM-based recommendation refinement and explanations

### ⏰ Background Jobs & Scheduling
- **Source Ingestion Job**: Runs every 24 hours
  - Pulls new content from all active sources
  - Generates embeddings for new resources
  - Indexes resources in vector database
- **Daily Feed Generation Job**: Runs daily at 2 AM
  - Builds personalized feeds for each content type
  - Leverages vector similarity + heuristic signals
  - Pre-generates recommendations for fast UI access

### ☁️ Infrastructure (Azure)
- **Azure Container Apps**: Serverless container hosting with auto-scaling
- **Azure AI Search**: Vector database for semantic similarity search
- **Azure OpenAI**: Embedding generation with text-embedding-3-small
- **Azure SQL Database**: Application data storage
- **Azure Key Vault**: Secure secrets management
- **Application Insights**: Monitoring and telemetry

## Solution / Project Layout

The solution is organized as multiple projects following a modular, layered approach:

```text
recommendation-system-for-learning/
├─ src/
│  ├─ Rsl.Api/              # ASP.NET Core REST API (HTTP endpoints)
│  ├─ Rsl.Core/             # Domain models, interfaces, core business rules
│  ├─ Rsl.Infrastructure/   # Persistence (EF Core), Azure AI Search, Azure OpenAI embeddings
│  ├─ Rsl.Jobs/             # Background jobs (source ingestion, daily feed generation)
│  ├─ Rsl.Recommendation/   # Hybrid recommendation engine (vector similarity + heuristics)
│  ├─ Rsl.Llm/              # LLM-based ingestion agent with function calling
│  └─ Rsl.Web/              # Blazor frontend (UI, pages, components)
│
├─ tests/
│  └─ Rsl.Tests/            # Unit and integration tests
│
├─ infrastructure/          # Azure deployment infrastructure
│  ├─ bicep/                # Azure Bicep IaC templates
│  └─ scripts/              # Deployment automation scripts
```
