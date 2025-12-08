using Microsoft.JSInterop;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing light/dark theme preference using DOM manipulation.
/// </summary>
public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode = false;

    public event Action? OnThemeChanged;

    public bool IsDarkMode => _isDarkMode;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        // Set initial theme on body element
        await ApplyThemeAsync();
    }

    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    public async Task SetThemeAsync(bool isDarkMode)
    {
        if (_isDarkMode != isDarkMode)
        {
            _isDarkMode = isDarkMode;
            await ApplyThemeAsync();
            OnThemeChanged?.Invoke();
        }
    }

    private async Task ApplyThemeAsync()
    {
        try
        {
            // Apply theme class directly to body element
            await _jsRuntime.InvokeVoidAsync("eval",
                $"document.body.className = '{(_isDarkMode ? "dark-mode" : "light-mode")}';");
        }
        catch
        {
            // JS interop might not be available during prerendering
        }
    }
}
