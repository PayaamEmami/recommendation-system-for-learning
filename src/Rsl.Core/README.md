# Rsl.Core

The domain layer for RSL. Contains business entities, repository interfaces, and core business rules.

## Purpose

Core is the **heart of the application** - it defines what the system does and what data it works with, completely independent of how that data is stored or accessed.

This project has **zero dependencies** on:
- Databases (no Entity Framework, no SQL)
- Web frameworks (no ASP.NET)
- External libraries (just pure .NET)

Everything else depends on Core. Core depends on nothing.

## Key Design Decisions

### Clean Architecture / Dependency Inversion

Core defines interfaces (contracts) but doesn't implement them:

```
Rsl.Core (Interfaces)          Rsl.Infrastructure (Implementations)
├─ IUserRepository      ←─     UserRepository
├─ IResourceRepository  ←─     ResourceRepository
└─ ITopicRepository     ←─     TopicRepository
```

**Why?** Core defines the contract ("I need a way to get users"), and Infrastructure provides the implementation ("from SQL Server"). Core remains agnostic to data access technology.

Benefits:
- Core can be tested without a database
- Database providers can be swapped without modifying Core
- Business logic remains decoupled from infrastructure concerns

### Entities Are Infrastructure-Agnostic

Core entities are plain C# classes without database annotations. All database mapping configuration lives in Infrastructure, not Core.

### Resource Inheritance Hierarchy

All learning resources inherit from an abstract `Resource` base class:

```
Resource (abstract)
├─ Paper
├─ Video
├─ BlogPost
├─ CurrentEvent
└─ SocialMediaPost
```

**Why inheritance?** Resources share most properties (Title, Url, Description, PublishedDate) and have a natural "is-a" relationship. This enables polymorphic queries and unified navigation properties.

**Trade-off:** Derived types have some null properties (e.g., `DOI` only exists for Papers).

### Repository Interfaces

Core defines repository interfaces; Infrastructure implements them. This allows the API and other consumers to depend on abstractions rather than concrete implementations.

### Topics Are Read-Only

`ITopicRepository` intentionally omits Create/Update/Delete operations. Topics are pre-seeded via database migrations and users select from the existing list. This prevents inconsistent user-generated topics and maintains data quality.

## Entity Relationships

**Many-to-Many:**
- User ↔ Topic
- Resource ↔ Topic

**One-to-Many:**
- User → ResourceVote
- User → Recommendation
- Resource → ResourceVote
- Resource → Recommendation

## Enums

**ResourceType:** Discriminator for resource inheritance (used in Table-Per-Hierarchy mapping and feed separation)

**VoteType:** User feedback stored as integers (Downvote = -1, Upvote = 1)

## Core Boundaries

Core contains only domain entities, enums, and repository interfaces. Database configurations, HTTP concerns, authentication, logging, and external API clients belong in other layers.

