using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;

namespace Rsl.Infrastructure.Repositories;

/// <summary>
/// Implementation of IRecommendationRepository using Entity Framework Core.
/// </summary>
public class RecommendationRepository : IRecommendationRepository
{
    private readonly RslDbContext _context;

    public RecommendationRepository(RslDbContext context)
    {
        _context = context;
    }

    public async Task<Recommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Include(r => r.User)
            .Include(r => r.Resource)
                .ThenInclude(res => res.Topics)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Where(r => r.UserId == userId && r.Date == date)
            .Include(r => r.Resource)
                .ThenInclude(res => res.Topics)
            .OrderBy(r => r.FeedType)
            .ThenBy(r => r.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> GetByUserDateAndTypeAsync(Guid userId, DateOnly date, ResourceType feedType, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Where(r => r.UserId == userId && r.Date == date && r.FeedType == feedType)
            .Include(r => r.Resource)
                .ThenInclude(res => res.Topics)
            .OrderBy(r => r.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> GetHistoryForUserAsync(Guid userId, ResourceType? feedType = null, int pageSize = 30, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var query = _context.Recommendations
            .Where(r => r.UserId == userId);

        if (feedType.HasValue)
        {
            query = query.Where(r => r.FeedType == feedType.Value);
        }

        return await query
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.Position)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Resource)
                .ThenInclude(res => res.Topics)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> CreateBatchAsync(IEnumerable<Recommendation> recommendations, CancellationToken cancellationToken = default)
    {
        var recommendationsList = recommendations.ToList();
        await _context.Recommendations.AddRangeAsync(recommendationsList, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return recommendationsList;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recommendation = await _context.Recommendations.FindAsync(new object[] { id }, cancellationToken);
        if (recommendation != null)
        {
            _context.Recommendations.Remove(recommendation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> HasRecommendationsForDateAsync(Guid userId, DateOnly date, ResourceType feedType, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .AnyAsync(r => r.UserId == userId && r.Date == date && r.FeedType == feedType, cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> GetRecentByUserAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Where(r => r.UserId == userId && r.Date >= startDate && r.Date <= endDate)
            .Include(r => r.Resource)
                .ThenInclude(res => res.Topics)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default)
    {
        _context.Recommendations.Add(recommendation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

