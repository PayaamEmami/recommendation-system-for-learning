using Rsl.Api.DTOs.Common;
using Rsl.Api.DTOs.Resources.Requests;
using Rsl.Api.DTOs.Resources.Responses;
using Rsl.Api.DTOs.Sources.Responses;
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
    private readonly ISourceRepository _sourceRepository;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(
        IResourceRepository resourceRepository,
        ISourceRepository sourceRepository,
        ILogger<ResourceService> logger)
    {
        _resourceRepository = resourceRepository;
        _sourceRepository = sourceRepository;
        _logger = logger;
    }

    public async Task<PagedResponse<ResourceResponse>> GetResourcesAsync(
        int pageNumber,
        int pageSize,
        ResourceType? type = null,
        List<Guid>? sourceIds = null,
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

        // Apply source filter if specified
        if (sourceIds != null && sourceIds.Any())
        {
            resources = resources.Where(r => r.SourceId.HasValue && sourceIds.Contains(r.SourceId.Value));
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
        // Validate source if provided
        if (request.SourceId.HasValue)
        {
            var source = await _sourceRepository.GetByIdAsync(request.SourceId.Value, cancellationToken);
            if (source == null)
            {
                throw new ArgumentException($"Source with ID {request.SourceId} not found");
            }
        }

        // Create resource based on type
        Resource resource = request.ResourceType switch
        {
            ResourceType.Paper => new Paper(),
            ResourceType.Video => new Video(),
            ResourceType.BlogPost => new BlogPost(),
            _ => throw new ArgumentException($"Invalid resource type: {request.ResourceType}")
        };

        // Set common properties
        resource.Id = Guid.NewGuid();
        resource.Title = request.Title;
        resource.Description = request.Description;
        resource.Url = request.Url;
        resource.SourceId = request.SourceId;
        resource.CreatedAt = DateTime.UtcNow;
        resource.UpdatedAt = DateTime.UtcNow;

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

        // Update source if provided
        if (request.SourceId.HasValue)
        {
            var source = await _sourceRepository.GetByIdAsync(request.SourceId.Value, cancellationToken);
            if (source == null)
            {
                throw new ArgumentException($"Source with ID {request.SourceId} not found");
            }
            resource.SourceId = request.SourceId;
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
            Type = resource.Type,
            CreatedAt = resource.CreatedAt,
            UpdatedAt = resource.UpdatedAt,
            SourceInfo = resource.Source != null ? new SourceResponse
            {
                Id = resource.Source.Id,
                UserId = resource.Source.UserId,
                Name = resource.Source.Name,
                Url = resource.Source.Url,
                Description = resource.Source.Description,
                Category = resource.Source.Category,
                IsActive = resource.Source.IsActive,
                CreatedAt = resource.Source.CreatedAt,
                UpdatedAt = resource.Source.UpdatedAt,
                ResourceCount = resource.Source.Resources?.Count ?? 0
            } : null
        };
    }
}

