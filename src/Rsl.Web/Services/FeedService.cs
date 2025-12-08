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
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Add some sample resources for demonstration
        _resources.AddRange(new[]
        {
            new ResourceItem
            {
                Id = Guid.NewGuid(),
                Title = "Attention Is All You Need",
                Url = "https://arxiv.org/abs/1706.03762",
                Type = ResourceType.Paper,
                Description = "The seminal transformer paper that revolutionized NLP",
                PublishedAt = DateTime.UtcNow.AddDays(-30)
            },
            new ResourceItem
            {
                Id = Guid.NewGuid(),
                Title = "3Blue1Brown - Neural Networks",
                Url = "https://www.youtube.com/watch?v=aircAruvnKk",
                Type = ResourceType.Video,
                Description = "A visual introduction to neural networks",
                PublishedAt = DateTime.UtcNow.AddDays(-15)
            },
            new ResourceItem
            {
                Id = Guid.NewGuid(),
                Title = "Understanding RLHF in LLMs",
                Url = "https://huggingface.co/blog/rlhf",
                Type = ResourceType.BlogPost,
                Description = "A comprehensive guide to reinforcement learning from human feedback",
                PublishedAt = DateTime.UtcNow.AddDays(-7)
            },
            new ResourceItem
            {
                Id = Guid.NewGuid(),
                Title = "OpenAI Announces GPT-4 Turbo",
                Url = "https://openai.com/blog/gpt-4-turbo",
                Type = ResourceType.CurrentEvent,
                Description = "Latest updates from OpenAI on their GPT-4 model",
                PublishedAt = DateTime.UtcNow.AddDays(-2)
            },
            new ResourceItem
            {
                Id = Guid.NewGuid(),
                Title = "Andrew Ng on AI Education",
                Url = "https://twitter.com/andrewng/status/123",
                Type = ResourceType.SocialMediaPost,
                Description = "Thoughts on democratizing AI education",
                PublishedAt = DateTime.UtcNow.AddHours(-12)
            }
        });
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
