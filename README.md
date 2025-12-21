# Recommendation System for Learning (RSL)

**Recommendation System for Learning (RSL)** is a personalized recommendation system designed to aggregate and suggest relevant learning resources from user-defined sources.

RSL helps you stay up-to-date with the latest content from your favorite learning sources—whether that's research papers, YouTube channels, technical blogs, or newsletters. Instead of manually checking multiple websites, RSL automatically ingests new content daily and uses AI-powered recommendations to surface the most relevant resources based on your interests and preferences. The system combines semantic search with collaborative filtering to deliver personalized daily feeds across different content types.

## Overview

RSL provides:

- **Add and manage URL-based sources** (RSS feeds, YouTube channels, blogs, newsletters, etc.) organized by content category.
- **Automatically ingest and aggregate** learning resources from these sources using LLM-powered agents.
- **Provide personalized feeds** for different content types:
  - Papers
  - Videos
  - Blogs
- **Hybrid recommendation engine** combining vector embeddings with heuristic signals for personalized content discovery.
- **Vote on resources** (upvote/downvote) to refine recommendations based on your preferences.

## Technology Stack

### Backend
- **.NET 10** with C#
- **ASP.NET Core** for REST API
- **Entity Framework Core** for data access
- **Blazor Server** for interactive web UI

### Cloud & Infrastructure (Azure)
- **Azure Container Apps** - Serverless container hosting with auto-scaling
- **Azure Container Apps Jobs** - Scheduled cron-based job execution
- **Azure AI Search** - Vector database for semantic similarity search
- **Azure OpenAI** - GPT-4 and text-embedding-3-small models
- **Azure SQL Database** - Application data storage
- **Azure Container Registry** - Container image storage
- **Azure Key Vault** - Secure secrets management
- **Application Insights** - Monitoring and telemetry

### AI & Machine Learning
- **Vector Embeddings** (text-embedding-3-small)
- **Semantic Search** via Azure AI Search
- **LLM Agents** with function calling (GPT-4)
- **Hybrid Recommendation Engine** (70% vector similarity, 30% heuristics)

### DevOps & Deployment
- **Docker** - Containerization
- **GitHub Actions** - CI/CD pipelines
- **Azure Bicep** - Infrastructure as Code

### Security & Authentication
- **JWT Authentication** - JSON Web Tokens
- **Password Hashing** - ASP.NET Core Identity
- **Rate Limiting** - API throttling
- **CORS** - Cross-origin configuration

## High-Level Architecture

At a high level, RSL is composed of:

### 🎨 Blazor Server Frontend
- Interactive web UI with multiple feed types (Papers, Videos, Blogs)
- User flows for browsing personalized feeds and managing sources
- Real-time updates and responsive design
- Dark/Light theme support

### 🔧 .NET Backend + REST API
- Central application layer (business logic, validation, orchestration)
- JWT-based authentication with refresh tokens
- API versioning and rate limiting
- REST endpoints for:
  - User authentication and registration
  - Source management (URL-based content sources)
  - Resource aggregation (content items)
  - Resource voting (upvote/downvote)
  - Personalized recommendations

### 📡 Data Ingestion Layer
- Pulls content from user-configured sources:
  - RSS/Atom feeds (blogs, papers, news)
  - YouTube channels
  - Newsletter integrations
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
  - Automatically categorizes resources (Papers, Videos, Blogs, etc.)
  - Extracts metadata and handles duplicate detection
  - Flexible, no custom parsers needed per source

### ⏰ Background Jobs & Scheduling
Jobs are implemented as **Azure Container Apps Jobs** with cron scheduling:

- **Source Ingestion Job**: Runs daily at midnight UTC
  - Pulls new content from all active sources using LLM agent
  - Generates embeddings for new resources via Azure OpenAI
  - Indexes resources in Azure AI Search vector database
  - Handles duplicate detection automatically
  - Container only runs during execution (cost-efficient)

- **Daily Feed Generation Job**: Runs daily at 2 AM UTC
  - Builds personalized feeds for each content type (5 recommendations per category)
  - Leverages vector similarity + heuristic signals
  - Pre-generates recommendations for fast UI access
  - Filters out already-seen and recently-recommended content
  - Container only runs during execution (cost-efficient)

**Benefits**: No retries on failure to prevent repeated API calls, schedule visible in Azure Portal, can be triggered manually

### ☁️ Infrastructure (Azure)
- **Azure Container Apps**: Serverless container hosting for API and Web (auto-scaling)
- **Azure Container Apps Jobs**: Scheduled job execution with cron triggers (cost-efficient)
- **Azure AI Search**: Vector database for semantic similarity search
- **Azure OpenAI**: Embedding generation with text-embedding-3-small and GPT-4
- **Azure SQL Database**: Application data storage
- **Azure Container Registry**: Docker image storage
- **Azure Key Vault**: Secure secrets management
- **Application Insights**: Monitoring and telemetry

### 🚀 Deployment & CI/CD
- **Containerized Microservices**:
  - 2 Container Apps (API, Web) - Always running
  - 2 Container Apps Jobs (Ingestion, Feed Generation) - Scheduled execution
- **GitHub Actions**: Automated CI/CD pipeline
  - Builds and tests on every push
  - Pushes Docker images to Azure Container Registry
  - Deploys to Azure Container Apps and Jobs automatically
- **Infrastructure as Code**: Azure Bicep templates for reproducible deployments
- **Database Migrations**: Automated via EF Core on startup

## Solution / Project Layout

The solution is organized as multiple projects following a modular, layered approach:

```text
recommendation-system-for-learning/
├─ src/
│  ├─ Rsl.Api/              # ASP.NET Core REST API (HTTP endpoints, controllers)
│  ├─ Rsl.Core/             # Domain models, entities, interfaces, enums
│  ├─ Rsl.Infrastructure/   # Data access (EF Core), Azure AI Search, Azure OpenAI
│  ├─ Rsl.Jobs/             # Background workers (source ingestion, feed generation)
│  ├─ Rsl.Recommendation/   # Recommendation engine (scoring, filtering, personalization)
│  ├─ Rsl.Llm/              # LLM-based ingestion agent with function calling
│  └─ Rsl.Web/              # Blazor Server frontend (UI, pages, components)
│
├─ tests/
│  └─ Rsl.Tests/            # Unit and integration tests
│
├─ infrastructure/          # Azure deployment infrastructure
│  ├─ bicep/                # Azure Bicep IaC templates
│  └─ scripts/              # Deployment automation scripts
│
├─ .github/
│  └─ workflows/            # GitHub Actions CI/CD pipelines
```

## Architecture Principles

- **Clean Architecture**: Separation of concerns with clear dependencies
- **Repository Pattern**: Abstraction over data access
- **Dependency Injection**: Loose coupling and testability
- **SOLID Principles**: Maintainable and extensible code
- **Domain-Driven Design**: Rich domain models with behavior
