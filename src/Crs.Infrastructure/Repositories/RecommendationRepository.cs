using Microsoft.EntityFrameworkCore;
using Crs.Core.Entities;
using Crs.Core.Enums;
using Crs.Core.Interfaces;
using Crs.Infrastructure.Data;

namespace Crs.Infrastructure.Repositories;

/// <summary>
/// Implementation of IRecommendationRepository using Entity Framework Core.
/// </summary>
public class RecommendationRepository : IRecommendationRepository
{
    private readonly CrsDbContext _context;

    public RecommendationRepository(CrsDbContext context)
    {
        _context = context;
    }

    public async Task<Recommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Include(r => r.User)
            .Include(r => r.Content)
                .ThenInclude(res => res.Source)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Where(r => r.UserId == userId && r.Date == date)
            .Include(r => r.Content)
                .ThenInclude(res => res.Source)
            .OrderBy(r => r.FeedType)
            .ThenBy(r => r.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> GetByUserDateAndTypeAsync(Guid userId, DateOnly date, ContentType feedType, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Where(r => r.UserId == userId && r.Date == date && r.FeedType == feedType)
            .Include(r => r.Content)
                .ThenInclude(res => res.Source)
            .OrderBy(r => r.Position)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> GetHistoryForUserAsync(Guid userId, ContentType? feedType = null, int pageSize = 30, int pageNumber = 1, CancellationToken cancellationToken = default)
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
            .Include(r => r.Content)
                .ThenInclude(res => res.Source)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> CreateBatchAsync(IEnumerable<Recommendation> recommendations, CancellationToken cancellationToken = default)
    {
        var recommendationsList = recommendations.ToList();
        await _context.Recommendations.AddRangeAsync(recommendationsList, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return recommendationsList;
    }

    public async Task ReplaceFeedAsync(
        Guid userId,
        DateOnly date,
        ContentType feedType,
        IEnumerable<Recommendation> recommendations,
        CancellationToken cancellationToken = default)
    {
        var recommendationsList = recommendations.ToList();
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var existing = await _context.Recommendations
                .Where(r => r.UserId == userId && r.Date == date && r.FeedType == feedType)
                .ToListAsync(cancellationToken);

            if (existing.Any())
            {
                _context.Recommendations.RemoveRange(existing);
            }

            if (recommendationsList.Any())
            {
                await _context.Recommendations.AddRangeAsync(recommendationsList, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
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

    public async Task<bool> HasRecommendationsForDateAsync(Guid userId, DateOnly date, ContentType feedType, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .AnyAsync(r => r.UserId == userId && r.Date == date && r.FeedType == feedType, cancellationToken);
    }

    public async Task<IEnumerable<Recommendation>> GetRecentByUserAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Where(r => r.UserId == userId && r.Date >= startDate && r.Date <= endDate)
            .Include(r => r.Content)
                .ThenInclude(res => res.Source)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default)
    {
        _context.Recommendations.Add(recommendation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DateOnly?> GetMostRecentDateWithRecommendationsAsync(
        Guid userId,
        ContentType feedType,
        CancellationToken cancellationToken = default)
    {
        return await _context.Recommendations
            .Where(r => r.UserId == userId && r.FeedType == feedType)
            .OrderByDescending(r => r.Date)
            .Select(r => (DateOnly?)r.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
