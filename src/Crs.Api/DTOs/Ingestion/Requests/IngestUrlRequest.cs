using System.ComponentModel.DataAnnotations;

namespace Crs.Api.DTOs.Ingestion.Requests;

/// <summary>
/// Request to ingest content from a URL using the LLM agent.
/// </summary>
public class IngestUrlRequest
{
    /// <summary>
    /// The URL to ingest content from.
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
}

