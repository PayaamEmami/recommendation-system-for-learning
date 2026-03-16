using Crs.Core.Entities;
using Crs.Core.Interfaces;

namespace Crs.Tests.Unit.Infrastructure;

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Dictionary<string, RefreshToken> _tokens = new();

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _tokens[token.Token] = token;
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        var removed = _tokens.TryGetValue(token, out var entity);
        if (removed)
        {
            _tokens.Remove(token);
        }
        return Task.FromResult(removed ? entity : null);
    }

    public Task RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;
        foreach (var kvp in _tokens.Where(x => x.Value.ExpiresAt <= cutoff).ToList())
        {
            _tokens.Remove(kvp.Key);
        }
        return Task.CompletedTask;
    }
}
