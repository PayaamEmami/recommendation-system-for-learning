using System.ComponentModel.DataAnnotations;
using Rsl.Core.Enums;

namespace Rsl.Api.DTOs.Requests;

/// <summary>
/// Request model for voting on a resource.
/// </summary>
public class VoteRequest
{
    /// <summary>
    /// The type of vote (Upvote or Downvote).
    /// </summary>
    [Required(ErrorMessage = "Vote type is required")]
    public VoteType VoteType { get; set; }
}

