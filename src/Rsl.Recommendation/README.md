# Rsl.Recommendation

The **Recommendation Engine** for the RSL (Recommendation System for Learning) project.

## Overview

This project contains the core recommendation logic that generates personalized learning resource recommendations based on user preferences, interaction history, and content metadata.

## Architecture

### Hybrid Recommendation Engine

The system uses a **hybrid recommendation approach** combining semantic similarity with traditional content-based signals:

```
User Embedding (from upvotes) → Vector Search → Heuristic Scoring → Filtering → Ranked Recommendations
                                      ↓
                            OpenSearch (Vector DB)
```

**Primary Signal (70% weight)**: Vector similarity using embeddings
**Secondary Signals (30% weight)**: Recency, source preferences, vote history

### Components

#### 1. **Models**

- `UserInterestProfile` - User preference representation
  - `UserEmbedding` - Aggregated embedding vector from upvoted resources (primary)
  - `TopicScores` - Source preference scores (legacy, used by heuristic scorers)
- `ScoredResource` - Resource with calculated recommendation scores
- `RecommendationContext` - Context for generating recommendations

#### 2. **Engine**

- `HybridRecommendationEngine` - Main orchestrator:
  1. **Vector Search Phase**: Get candidates via semantic similarity using user embedding
  2. **Heuristic Scoring Phase**: Apply traditional signals (recency, source, votes)
  3. **Filtering Phase**: Remove duplicates, ensure diversity
  4. **Ranking Phase**: Combine scores (70% vector + 30% heuristic) and sort

#### 3. **Scorers** (Heuristic Signals)

- `SourceScorer` (50% of heuristic weight) - Matches resources to user's preferred sources
- `RecencyScorer` (30% of heuristic weight) - Exponential decay favoring newer content
- `VoteHistoryScorer` (20% of heuristic weight) - Scores based on voting patterns
- `CompositeScorer` - Combines heuristic scorers into weighted score

#### 4. **Filters**

- `SeenResourceFilter` - Removes already-seen and recently-recommended resources
- `DiversityFilter` - Ensures source diversity, prevents over-representation

#### 5. **Services**

- `UserProfileService` - Builds user profiles:
  - Generates user embedding by averaging embeddings of upvoted resources
  - Calculates source preference scores for legacy scorers
- `FeedGenerator` - Generates and persists daily recommendation feeds

## How It Works

### Daily Feed Generation

1. **Build User Profile**:
   - Aggregate embeddings of all upvoted resources → User embedding vector
   - Calculate source preference scores from voting history (for heuristic scorers)
2. **Vector Search**: Query OpenSearch for semantically similar resources
   - Uses user embedding as query vector
   - Applies filters: resource type, recency (90 days), exclude seen/recommended
   - Returns top candidates with similarity scores
3. **Heuristic Scoring**: Apply traditional signals to vector candidates
   - Recency: Exponential decay favoring newer content
   - Source preference: Boost resources from user's preferred sources
   - Vote history: Consider patterns in user's voting behavior
4. **Combine Scores**: Hybrid ranking
   - 70% weight on vector similarity
   - 30% weight on combined heuristic signals
5. **Apply Filters**: Remove seen resources, ensure diversity
6. **Rank & Select**: Sort by final score and select top N
7. **Persist**: Save recommendations to database

### Scoring Algorithm

Each resource receives a hybrid score:

```
Final Score = (VectorSimilarity × 0.7) + (HeuristicScore × 0.3)

where HeuristicScore = (SourceScore × 0.5) + (RecencyScore × 0.3) + (VoteHistoryScore × 0.2)
```

- **VectorSimilarity**: Cosine similarity between user embedding and resource embedding
- **SourceScore**: User's preference for the resource's source
- **RecencyScore**: Exponential decay (e^(-age/30 days))
- **VoteHistoryScore**: Based on voting patterns for similar sources

## Usage

### Register Services

```csharp
builder.Services.AddRecommendationEngine();
```

### Generate Recommendations

```csharp
// Inject IFeedGenerator
var feedGenerator = serviceProvider.GetRequiredService<IFeedGenerator>();

// Generate recommendations for a specific feed
var recommendations = await feedGenerator.GenerateFeedAsync(
    userId: userId,
    feedType: ResourceType.Paper,
    date: DateOnly.FromDateTime(DateTime.UtcNow),
    count: 5
);

// Or generate all feeds at once
var allRecommendations = await feedGenerator.GenerateAllFeedsAsync(
    userId: userId,
    date: DateOnly.FromDateTime(DateTime.UtcNow)
);
```

## Dependencies

The recommendation engine integrates with:

- **OpenSearch** (via `IVectorStore`) - Semantic similarity search
- **OpenAI** (via `IEmbeddingService`) - Text embedding generation
- **PostgreSQL** (via EF Core repositories) - Persistence

## Future Enhancements

### Phase 2: Advanced Features

- **Multi-modal embeddings**: Combine text with metadata (authors, topics, citations)
- **Temporal dynamics**: Weight recent upvotes more heavily in user embedding
- **Collaborative signals**: Leverage similar users' preferences
- **Fine-tuned embeddings**: Domain-specific embedding models for academic content

### Phase 3: LLM Integration

- **Explanation generation**: LLM-generated reasons for each recommendation
- **Query refinement**: Natural language queries to adjust recommendations
- **Study plan generation**: Personalized learning paths based on goals

## Cold Start Problem

The engine handles cold start gracefully:

1. **No User History**: Returns neutral scores, prioritizes recent content
2. **Few Interactions**: Gradually builds profile as user votes
3. **No Resources**: Returns empty list with appropriate logging

## Configuration

Default settings:

- Feed count: 5 recommendations per feed
- Candidate window: Last 90 days
- Diversity limit: Max 3 resources per source
- Recent recommendations window: Last 7 days
- Recency half-life: 30 days

These can be adjusted in the respective scorer/filter implementations.

## Configuration

The engine requires:

- OpenSearch configured with vector index (via `IVectorStore`)
- OpenAI embeddings service (via `IEmbeddingService`)
- Database with user votes and resources

See `Rsl.Infrastructure` for configuration details.

## Testing

See `Rsl.Tests` project for unit and integration tests.
