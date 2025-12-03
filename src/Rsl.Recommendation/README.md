# Rsl.Recommendation

The **Recommendation Engine** for the RSL (Recommendation System for Learning) project.

## Overview

This project contains the core recommendation logic that generates personalized learning resource recommendations based on user preferences, interaction history, and content metadata.

## Architecture

### Phase 1: Content-Based Engine (Current)

The current implementation uses a **content-based recommendation approach** that works immediately without historical data:

```
User Interest Profile → Resource Scoring → Filtering → Ranked Recommendations
```

### Components

#### 1. **Models**
- `UserInterestProfile` - Tracks user's source preferences based on voting history
- `ScoredResource` - Resource with calculated recommendation scores
- `RecommendationContext` - Context for generating recommendations

#### 2. **Scorers**
- `SourceScorer` (50% weight) - Matches resources to user's preferred sources
- `RecencyScorer` (30% weight) - Boosts newer content with exponential decay
- `VoteHistoryScorer` (20% weight) - Scores based on sources of upvoted content
- `CompositeScorer` - Combines all scorers into weighted final score

#### 3. **Filters**
- `SeenResourceFilter` - Removes already-seen and recently-recommended resources
- `DiversityFilter` - Ensures source diversity, prevents over-representation

#### 4. **Engine**
- `RecommendationEngine` - Orchestrates the recommendation pipeline:
  1. Fetch candidate resources
  2. Score all candidates
  3. Apply filters
  4. Return top N ranked recommendations

#### 5. **Services**
- `UserProfileService` - Builds user interest profiles from voting history
- `FeedGenerator` - Generates and persists daily recommendation feeds

## How It Works

### Daily Feed Generation

1. **Build User Profile**: Analyze voting history to determine source preferences
2. **Fetch Candidates**: Get recent resources of the specified feed type (last 90 days)
3. **Score Resources**: Calculate scores based on source preference, recency, and vote history
4. **Apply Filters**: Remove seen resources and ensure diversity
5. **Rank & Select**: Sort by score and select top N
6. **Persist**: Save recommendations to database with position and scores

### Scoring Algorithm

Each resource receives a weighted score:

```
Final Score = (SourceScore × 0.5) + (RecencyScore × 0.3) + (VoteHistoryScore × 0.2)
```

- **SourceScore**: User's interest score for the resource's source (based on voting history)
- **RecencyScore**: Exponential decay (e^(-age/30 days))
- **VoteHistoryScore**: Positive score if source has upvotes, negative if downvotes

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

## Future Enhancements

### Phase 2: ML.NET Integration
- Add collaborative filtering using matrix factorization
- Periodic model retraining
- Hybrid scoring (content + collaborative)

### Phase 3: LLM Layer
- Context-aware filtering and reranking
- Natural language explanations
- Personalized study plan generation

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

## Dependencies

- `Rsl.Core` - Domain models and interfaces
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`

## Testing

See `Rsl.Tests` project for unit and integration tests.

