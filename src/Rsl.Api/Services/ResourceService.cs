using Rsl.Api.DTOs.Requests;
using Rsl.Api.DTOs.Responses;
using Rsl.Core.Entities;
using Rsl.Core.Enums;
using Rsl.Core.Interfaces;

namespace Rsl.Api.Services;

/// <summary>
/// Service for handling resource-related operations.
/// </summary>
public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(
        IResourceRepository resourceRepository,
        ITopicRepository topicRepository,
        ILogger<ResourceService> logger)
    {
        _resourceRepository = resourceRepository;
        _topicRepository = topicRepository;
        _logger = logger;
    }

    public async Task<PagedResponse<ResourceResponse>> GetResourcesAsync(
        int pageNumber,
        int pageSize,
        ResourceType? type = null,
        List<Guid>? topicIds = null,
        CancellationToken cancellationToken = default)
    {
        // Get resources (apply type filter if specified)
        IEnumerable<Resource> resources;

        if (type.HasValue)
        {
            resources = await _resourceRepository.GetByTypeAsync(type.Value, cancellationToken);
        }
        else
        {
            resources = await _resourceRepository.GetAllAsync(cancellationToken);
        }

        // Apply topic filter if specified
        if (topicIds != null && topicIds.Any())
        {
            resources = resources.Where(r => r.Topics.Any(t => topicIds.Contains(t.Id)));
        }

        // Get total count before pagination
        var totalCount = resources.Count();

        // Apply pagination
        var pagedResources = resources
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<ResourceResponse>
        {
            Items = pagedResources.Select(MapToResourceResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ResourceResponse?> GetResourceByIdAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByIdAsync(resourceId, cancellationToken);

        if (resource == null)
        {
            return null;
        }

        return MapToResourceResponse(resource);
    }

    public async Task<ResourceResponse> CreateResourceAsync(
        CreateResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        // Fetch topics
        var topics = new List<Topic>();
        foreach (var topicId in request.TopicIds)
        {
            var topic = await _topicRepository.GetByIdAsync(topicId, cancellationToken);
            if (topic == null)
            {
                throw new ArgumentException($"Topic with ID {topicId} not found");
            }
            topics.Add(topic);
        }

        // Create resource based on type
        Resource resource = request.ResourceType switch
        {
            ResourceType.Paper => new Paper(),
            ResourceType.Video => new Video(),
            ResourceType.BlogPost => new BlogPost(),
            ResourceType.CurrentEvent => new CurrentEvent(),
            ResourceType.SocialMediaPost => new SocialMediaPost(),
            _ => throw new ArgumentException($"Invalid resource type: {request.ResourceType}")
        };

        // Set common properties
        resource.Id = Guid.NewGuid();
        resource.Title = request.Title;
        resource.Description = request.Description;
        resource.Url = request.Url;
        resource.PublishedDate = request.PublishedDate;
        resource.Source = request.Source;
        resource.CreatedAt = DateTime.UtcNow;
        resource.UpdatedAt = DateTime.UtcNow;
        resource.Topics = topics;

        await _resourceRepository.CreateAsync(resource, cancellationToken);

        _logger.LogInformation("Created resource {ResourceId} of type {ResourceType}", resource.Id, resource.Type);

        return MapToResourceResponse(resource);
    }

    public async Task<ResourceResponse> UpdateResourceAsync(
        Guid resourceId,
        UpdateResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByIdAsync(resourceId, cancellationToken);
        if (resource == null)
        {
            throw new KeyNotFoundException($"Resource with ID {resourceId} not found");
        }

        // Update fields if provided
        if (request.Title != null)
        {
            resource.Title = request.Title;
        }

        if (request.Description != null)
        {
            resource.Description = request.Description;
        }

        if (request.Url != null)
        {
            resource.Url = request.Url;
        }

        if (request.PublishedDate.HasValue)
        {
            resource.PublishedDate = request.PublishedDate;
        }

        if (request.Source != null)
        {
            resource.Source = request.Source;
        }

        // Update topics if provided
        if (request.TopicIds != null)
        {
            var topics = new List<Topic>();
            foreach (var topicId in request.TopicIds)
            {
                var topic = await _topicRepository.GetByIdAsync(topicId, cancellationToken);
                if (topic == null)
                {
                    throw new ArgumentException($"Topic with ID {topicId} not found");
                }
                topics.Add(topic);
            }
            resource.Topics = topics;
        }

        resource.UpdatedAt = DateTime.UtcNow;

        await _resourceRepository.UpdateAsync(resource, cancellationToken);

        _logger.LogInformation("Updated resource {ResourceId}", resourceId);

        return MapToResourceResponse(resource);
    }

    public async Task DeleteResourceAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByIdAsync(resourceId, cancellationToken);
        if (resource == null)
        {
            throw new KeyNotFoundException($"Resource with ID {resourceId} not found");
        }

        await _resourceRepository.DeleteAsync(resourceId, cancellationToken);

        _logger.LogInformation("Deleted resource {ResourceId}", resourceId);
    }

    private static ResourceResponse MapToResourceResponse(Resource resource)
    {
        return new ResourceResponse
        {
            Id = resource.Id,
            Title = resource.Title,
            Description = resource.Description,
            Url = resource.Url,
            PublishedDate = resource.PublishedDate,
            Source = resource.Source,
            Type = resource.Type,
            CreatedAt = resource.CreatedAt,
            UpdatedAt = resource.UpdatedAt,
            Topics = resource.Topics.Select(t => new TopicResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList()
        };
    }
}

