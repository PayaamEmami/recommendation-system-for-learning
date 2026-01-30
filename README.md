# Recommendation System for Learning (RSL)

**Recommendation System for Learning (RSL)** aggregates learning resources from your chosen sources (papers, videos, technical blogs, newsletters, and more) and delivers a personalized daily feed. It automatically ingests new content, uses AI-driven ranking to surface what's most relevant to you, and lets you refine recommendations over time.

RSL started as a way to make it effortless to surface new learning resources (hence the name), but it grew into something broader: an **AI “buffer” between you and the internet**. It aggregates content from the sources you care about, filters out the noise, and delivers a small, intentional set of recommendations instead of an endless feed.

This helps solve a few common problems:

- **One place for everything**: consolidate many sources into a single daily feed, so you don't have to bounce between platforms.
- **Explicit, user-controlled recommendations**: steer the algorithm with simple upvotes/downvotes, rather than opaque tracking and "engagement" signals.
- **Built-in limits to prevent doom scrolling**: cap the amount of content surfaced so discovery stays focused and finite.

## Overview

RSL provides:

- **Add and manage URL-based sources** (RSS feeds, video sources, blogs, newsletters, etc.) organized by content category.
- **Automatically ingest and aggregate** learning resources from these sources using LLM-powered agents.
- **Provide personalized feeds** for different content types:
  - Papers
  - Videos
  - Blogs
- **Hybrid recommendation engine** combining vector embeddings with heuristic signals for personalized content discovery.
- **Vote on resources** (upvote/downvote) to refine recommendations based on your preferences.
- **Connect X accounts** to show a personalized X feed above recommendations (read-only, user selects followed accounts).

## Technology Stack

### Backend

- **.NET 10** with C#
- **ASP.NET Core** for REST API
- **Entity Framework Core** for data access
- **Blazor WebAssembly** for interactive web UI

### Cloud & Infrastructure (AWS)

- **Amazon S3 + CloudFront** - Static hosting for Blazor WebAssembly frontend
- **AWS App Runner** - Serverless container hosting for API
- **Amazon ECS Fargate + EventBridge** - Scheduled job execution
- **AWS OpenSearch Serverless** - Vector database for semantic similarity search
- **OpenAI API** - GPT-5-nano and text-embedding-3-small models
- **Amazon RDS PostgreSQL** - Application data storage
- **Amazon ECR** - Container image storage
- **AWS Secrets Manager** - Secure secrets management
- **Amazon CloudWatch** - Monitoring and logging

### AI & Machine Learning

- **Vector Embeddings** (text-embedding-3-small via OpenAI API)
- **Semantic Search** via AWS OpenSearch Serverless
- **LLM Agents** with function calling (GPT-5-nano via OpenAI API)
- **Hybrid Recommendation Engine** (70% vector similarity, 30% heuristics)

### DevOps & Deployment

- **Docker** - Containerization
- **GitHub Actions** - CI/CD pipelines
- **AWS CLI** - Infrastructure deployment

### Security & Authentication

- **JWT Authentication** - JSON Web Tokens
- **Password Hashing** - ASP.NET Core Identity
- **Rate Limiting** - API throttling
- **CORS** - Cross-origin configuration

## High-Level Architecture

At a high level, RSL is composed of:

### 🎨 Blazor WebAssembly Frontend

- Client-side interactive web UI hosted on S3 + CloudFront
- Multiple feed types (Papers, Videos, Blogs)
- User flows for browsing personalized feeds and managing sources
- Responsive design with Dark/Light theme support
- Offline-capable with local storage for auth persistence

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
  - Video sources
  - Newsletter integrations
- Parses and normalizes content into Resource entities
- Associates resources with their originating Source

### 🤖 Recommendation Engine

- **Hybrid recommendation system** combining:
  - **Vector similarity search** using text embeddings (primary signal, 70% weight)
    - Resources embedded using OpenAI embeddings (text-embedding-3-small)
    - Preferences represented as aggregated embeddings of upvoted content
    - Semantic similarity matching via AWS OpenSearch Serverless vector database
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

Jobs are implemented as **Amazon ECS Fargate tasks** with EventBridge cron scheduling:

- **Source Ingestion Job**: Runs daily at midnight UTC

  - Pulls new content from all active sources using LLM agent
  - Generates embeddings for new resources via OpenAI API
  - Indexes resources in AWS OpenSearch vector database
  - Handles duplicate detection automatically
  - Container only runs during execution (cost-efficient)

- **Daily Feed Generation Job**: Runs daily at 2 AM UTC
  - Builds personalized feeds for each content type (5 recommendations per category)
  - Leverages vector similarity + heuristic signals
  - Pre-generates recommendations for fast UI access
  - Filters out already-seen and recently-recommended content
  - Container only runs during execution (cost-efficient)

**Benefits**: No retries on failure to prevent repeated API calls, schedule visible in AWS Console, can be triggered manually via AWS CLI

### ☁️ Infrastructure (AWS)

- **Amazon S3 + CloudFront**: Static hosting for Blazor WebAssembly frontend (global CDN)
- **AWS App Runner**: Serverless container hosting for API (auto-scaling)
- **Amazon ECS Fargate + EventBridge**: Scheduled job execution with cron triggers (cost-efficient)
- **AWS OpenSearch Serverless**: Vector database for semantic similarity search
- **OpenAI API**: Embedding generation with text-embedding-3-small and GPT-5-nano
- **Amazon RDS PostgreSQL**: Application data storage
- **Amazon ECR**: Docker image storage
- **AWS Secrets Manager**: Secure secrets management
- **Amazon CloudWatch**: Monitoring and logging

### 🚀 Deployment & CI/CD

- **AWS Hosting**:
  - S3 + CloudFront (Web) - Global CDN distribution
  - App Runner (API) - Auto-scaling serverless
  - ECS Fargate + EventBridge (Jobs) - Scheduled execution
- **GitHub Actions**: Automated CI/CD pipeline
  - Builds and tests on every push
  - Pushes Docker images to Amazon ECR
  - Deploys API to App Runner
  - Deploys Web to S3 with CloudFront invalidation
- **Infrastructure as Code**: AWS CLI scripts for reproducible deployments
- **Database Migrations**: Automated via EF Core on startup

## Solution / Project Layout

The solution is organized as multiple projects following a modular, layered approach:

```text
recommendation-system-for-learning/
├─ src/
│  ├─ Rsl.Api/              # ASP.NET Core REST API (HTTP endpoints, controllers)
│  ├─ Rsl.Core/             # Domain models, entities, interfaces, enums
│  ├─ Rsl.Infrastructure/   # Data access (EF Core), OpenSearch, OpenAI
│  ├─ Rsl.Jobs/             # Background workers (source ingestion, feed generation)
│  ├─ Rsl.Recommendation/   # Recommendation engine (scoring, filtering, personalization)
│  ├─ Rsl.Llm/              # LLM-based ingestion agent with function calling
│  └─ Rsl.Web/              # Blazor WebAssembly frontend (UI, pages, components)
│
├─ tests/
│  └─ Rsl.Tests/            # Unit and integration tests
│
├─ infrastructure/          # AWS deployment infrastructure
│  └─ aws/                  # AWS CLI deployment scripts
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
