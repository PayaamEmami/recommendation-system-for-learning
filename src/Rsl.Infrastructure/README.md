# Rsl.Infrastructure

The infrastructure layer for RSL. Implements data access using Entity Framework Core and the repository pattern.

## Purpose

This project sits between the domain layer (`Rsl.Core`) and the database. It provides:
- Database context and entity configurations
- Concrete implementations of repository interfaces
- Data access concerns isolated from business logic

## Key Design Decisions

### Repository Pattern
All database operations go through repository interfaces defined in `Rsl.Core`. This provides:
- **Testability**: Easy to mock repositories in unit tests
- **Flexibility**: Can swap database implementations without changing business logic
- **Abstraction**: API layer doesn't need to know about EF Core or PostgreSQL

### Fluent API Over Data Annotations
Entity configurations use Fluent API (separate configuration classes) instead of attributes on domain models. This keeps `Rsl.Core` entities clean and database-agnostic.

### Table-Per-Hierarchy (TPH) Inheritance
All resource types (Paper, Video, BlogPost, etc.) are stored in a single `Resources` table with a discriminator column. This simplifies queries and foreign key relationships while still maintaining type safety in code.

**Why TPH over Table-Per-Type (TPT)?**
- Better query performance (no joins needed when querying all resources)
- Simpler foreign keys (recommendations/votes point to one table)
- Resource types share 80%+ of their fields

**Trade-off**: Some null columns for type-specific fields (e.g., `DOI` only exists for Papers).

## Database Schema Highlights

### Indexes
Performance-critical queries are optimized with indexes:
- `User.Email` (unique) - Login lookups
- `Topic.Slug` (unique) - URL-based topic queries
- `ResourceVote (UserId, ResourceId)` (composite unique) - Prevents duplicate votes
- `Recommendation (UserId, Date, FeedType)` (composite) - Fast feed retrieval

### Relationships & Cascade Behavior
- **Many-to-Many**: User ↔ Topics, Resource ↔ Topics (using join tables)
- **One-to-Many**: User → Votes (cascade delete), User → Recommendations (cascade delete)
- **One-to-Many**: Resource → Votes (cascade delete), Resource → Recommendations (restrict delete)

**Why restrict delete on Resource → Recommendations?**
Recommendations are historical records. If a resource is deleted, we want to preserve the fact that it was recommended (for metrics/auditing), not cascade delete the history.

### Unique Constraints
- **User.Email**: One account per email
- **Topic.Slug**: URL-friendly unique identifiers
- **ResourceVote (UserId, ResourceId)**: Users can't vote twice on same resource

## Usage

### Register in Dependency Injection
```csharp
// In Program.cs or Startup.cs
builder.Services.AddInfrastructure(builder.Configuration);
```

This registers:
- `RslDbContext` with PostgreSQL provider
- All repository implementations as scoped services

### Connection String Configuration
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=rsldb;Username=rsladmin;Password=..."
  }
}
```

### OpenSearch Configuration
```json
// appsettings.json
{
  "OpenSearch": {
    "Mode": "Local",
    "Endpoint": "http://localhost:9200",
    "IndexName": "rsl-resources",
    "EmbeddingDimensions": 1536,
    "Region": "us-west-2"
  }
}
```

**Mode options**:
- `Local`: No auth (Docker OpenSearch).
- `Aws`: Uses SigV4 with the configured `Region`.

### Migrations
```bash
# Create a new migration
dotnet ef migrations add MigrationName --startup-project ../Rsl.Api

# Apply migrations to database
dotnet ef database update --startup-project ../Rsl.Api
```

**Note**: Migrations require a startup project with the connection string configuration. Infrastructure project alone doesn't have an entry point.

## Important Notes

### Eager vs Lazy Loading
Repositories use explicit eager loading (`.Include()`) rather than lazy loading. This:
- Makes queries predictable and explicit
- Prevents N+1 query problems
- Makes it obvious what data is being loaded

### Async All The Way
All repository methods are async with `CancellationToken` support. This is non-negotiable for scalability.

### Connection Resilience
The DbContext is configured with `EnableRetryOnFailure()` for automatic retry on transient PostgreSQL errors (network blips, timeouts, etc.).
