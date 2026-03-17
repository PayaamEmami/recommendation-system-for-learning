using Crs.Core.Entities;

namespace Crs.Core.Interfaces;

/// <summary>
/// Repository interface for user-supplied manual content feedback.
/// </summary>
public interface IManualContentFeedbackRepository
{
    Task<ManualContentFeedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ManualContentFeedback?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ManualContentFeedback>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ManualContentFeedback> CreateAsync(ManualContentFeedback feedback, CancellationToken cancellationToken = default);
    Task<ManualContentFeedback> UpdateAsync(ManualContentFeedback feedback, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
