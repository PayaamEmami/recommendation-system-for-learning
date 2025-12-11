using Rsl.Core.Enums;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing feeds and resources.
/// In production, this would call the API layer.
/// </summary>
public class FeedService
{
    private readonly List<ResourceItem> _resources = new();
    private readonly AuthService _authService;

    public FeedService(AuthService authService)
    {
        _authService = authService;
        // Resources will be populated by the background ingestion job
        // after sources are added via the /sources page
    }

    public async Task<List<ResourceItem>> GetFeedAsync(ResourceType? type = null)
    {
        await Task.Delay(50); // Simulate async operation

        var query = _resources.AsEnumerable();

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        return query.OrderByDescending(r => r.PublishedAt).ToList();
    }
}

public class ResourceItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public string? Description { get; set; }
    public DateTime PublishedAt { get; set; }
}
