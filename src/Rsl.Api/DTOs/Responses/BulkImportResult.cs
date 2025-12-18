namespace Rsl.Api.DTOs.Responses;

/// <summary>
/// Response DTO for bulk import operation.
/// </summary>
public class BulkImportResult
{
    /// <summary>
    /// Number of sources successfully imported.
    /// </summary>
    public int Imported { get; set; }

    /// <summary>
    /// Number of sources that failed to import.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// List of errors for failed imports.
    /// </summary>
    public List<BulkImportError> Errors { get; set; } = new();
}

/// <summary>
/// Represents an error during bulk import.
/// </summary>
public class BulkImportError
{
    /// <summary>
    /// The URL that failed to import.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

