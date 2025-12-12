using Rsl.Core.Enums;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing user sources.
/// In production, this would call the API layer.
/// </summary>
public class SourceService
{
    private readonly List<SourceItem> _sources = new();
    private readonly AuthService _authService;

    public SourceService(AuthService authService)
    {
        _authService = authService;
    }

    public async Task<List<SourceItem>> GetUserSourcesAsync()
    {
        await Task.Delay(50); // Simulate async operation

        var userId = _authService.CurrentState.UserId;
        if (!userId.HasValue)
            return new List<SourceItem>();

        return _sources.Where(s => s.UserId == userId.Value).ToList();
    }

    public async Task<bool> AddSourceAsync(string name, string url, ResourceType category, string? description)
    {
        await Task.Delay(50); // Simulate async operation

        var userId = _authService.CurrentState.UserId;
        if (!userId.HasValue)
            return false;

        var source = new SourceItem
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Name = name,
            Url = url,
            Category = category,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _sources.Add(source);
        return true;
    }

    public async Task<bool> DeleteSourceAsync(Guid sourceId)
    {
        await Task.Delay(50); // Simulate async operation

        var userId = _authService.CurrentState.UserId;
        if (!userId.HasValue)
            return false;

        var source = _sources.FirstOrDefault(s => s.Id == sourceId && s.UserId == userId.Value);
        if (source == null)
            return false;

        _sources.Remove(source);
        return true;
    }

    public async Task<bool> ToggleSourceActiveAsync(Guid sourceId)
    {
        await Task.Delay(50); // Simulate async operation

        var userId = _authService.CurrentState.UserId;
        if (!userId.HasValue)
            return false;

        var source = _sources.FirstOrDefault(s => s.Id == sourceId && s.UserId == userId.Value);
        if (source == null)
            return false;

        source.IsActive = !source.IsActive;
        return true;
    }

    public async Task<bool> UpdateSourceAsync(Guid sourceId, string? name, string? url, ResourceType? category, string? description)
    {
        await Task.Delay(50); // Simulate async operation

        var userId = _authService.CurrentState.UserId;
        if (!userId.HasValue)
            return false;

        var source = _sources.FirstOrDefault(s => s.Id == sourceId && s.UserId == userId.Value);
        if (source == null)
            return false;

        if (!string.IsNullOrEmpty(name))
            source.Name = name;
        if (!string.IsNullOrEmpty(url))
            source.Url = url;
        if (category.HasValue)
            source.Category = category.Value;
        if (description != null)
            source.Description = description;

        return true;
    }
}

public class SourceItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ResourceType Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
