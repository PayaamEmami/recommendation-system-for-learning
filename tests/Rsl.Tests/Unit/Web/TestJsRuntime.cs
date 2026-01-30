using Microsoft.JSInterop;

namespace Rsl.Tests.Unit.Web;

internal sealed class TestJsRuntime : IJSRuntime
{
    public List<(string Identifier, object?[]? Args)> Calls { get; } = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Calls.Add((identifier, args));
        return new ValueTask<TValue>(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add((identifier, args));
        return new ValueTask<TValue>(default(TValue)!);
    }
}
