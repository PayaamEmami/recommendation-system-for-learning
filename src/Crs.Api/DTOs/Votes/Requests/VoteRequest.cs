using System.ComponentModel.DataAnnotations;
using Crs.Core.Enums;

namespace Crs.Api.DTOs.Votes.Requests;

/// <summary>
/// Request model for voting on a piece of content.
/// </summary>
public class VoteRequest
{
    /// <summary>
    /// The type of vote (Upvote or Downvote).
    /// </summary>
    [Required(ErrorMessage = "Vote type is required")]
    public VoteType VoteType { get; set; }
}
