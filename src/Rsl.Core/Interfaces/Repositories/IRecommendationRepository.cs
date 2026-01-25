using Rsl.Core.Entities;
using Rsl.Core.Enums;

namespace Rsl.Core.Interfaces;

/// <summary>
/// Repository interface for Recommendation entity operations.
/// </summary>
public interface IRecommendationRepository
{
    Task<Recommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Recommendation>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<Recommendation>> GetByUserDateAndTypeAsync(Guid userId, DateOnly date, ResourceType feedType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Recommendation>> GetHistoryForUserAsync(Guid userId, ResourceType? feedType = null, int pageSize = 30, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<Recommendation>> CreateBatchAsync(IEnumerable<Recommendation> recommendations, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasRecommendationsForDateAsync(Guid userId, DateOnly date, ResourceType feedType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Recommendation>> GetRecentByUserAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default);
    Task<DateOnly?> GetMostRecentDateWithRecommendationsAsync(Guid userId, ResourceType feedType, CancellationToken cancellationToken = default);
}

