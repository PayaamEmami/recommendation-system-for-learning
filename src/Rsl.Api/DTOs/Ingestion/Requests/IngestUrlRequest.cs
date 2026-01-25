using System.ComponentModel.DataAnnotations;

namespace Rsl.Api.DTOs.Ingestion.Requests;

/// <summary>
/// Request to ingest resources from a URL using the LLM agent.
/// </summary>
public class IngestUrlRequest
{
    /// <summary>
    /// The URL to ingest resources from.
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
}

