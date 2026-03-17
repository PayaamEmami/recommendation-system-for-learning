using Crs.Api.DTOs.Preferences.Requests;
using Crs.Api.DTOs.Preferences.Responses;

namespace Crs.Api.Services;

public interface IPreferenceService
{
    Task<List<ManualContentFeedbackResponse>> GetManualFeedbackAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ManualContentFeedbackResponse?> GetManualFeedbackByIdAsync(Guid userId, Guid feedbackId, CancellationToken cancellationToken = default);
    Task<ManualContentFeedbackResponse> CreateManualFeedbackAsync(Guid userId, CreateManualContentFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<ManualContentFeedbackResponse> UpdateManualFeedbackAsync(Guid userId, Guid feedbackId, UpdateManualContentFeedbackRequest request, CancellationToken cancellationToken = default);
    Task DeleteManualFeedbackAsync(Guid userId, Guid feedbackId, CancellationToken cancellationToken = default);
}
