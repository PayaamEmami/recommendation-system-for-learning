using Microsoft.JSInterop;

namespace Rsl.Web.Services;

/// <summary>
/// Service for managing light/dark theme preference with localStorage persistence.
/// </summary>
public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ThemeService> _logger;
    private IJSObjectReference? _themeModule;
    private bool _isDarkMode = false;
    private bool _isInitialized = false;

    public event Action? OnThemeChanged;

    public bool IsDarkMode => _isDarkMode;

    public ThemeService(IJSRuntime jsRuntime, ILogger<ThemeService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the theme service by loading the saved preference from localStorage.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Import the theme JavaScript module
            _themeModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/theme.js");

            // Load saved theme preference from localStorage
            _isDarkMode = await _themeModule.InvokeAsync<bool>("loadTheme");

            // Apply the loaded theme
            await ApplyThemeAsync();

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize theme service, using default light mode");
            // Default to light mode if initialization fails
            _isDarkMode = false;
        }
    }

    /// <summary>
    /// Toggles between light and dark mode and saves the preference.
    /// </summary>
    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await ApplyThemeAsync();
        await SaveThemeAsync();
        OnThemeChanged?.Invoke();
    }

    /// <summary>
    /// Sets the theme to a specific mode and saves the preference.
    /// </summary>
    public async Task SetThemeAsync(bool isDarkMode)
    {
        if (_isDarkMode != isDarkMode)
        {
            _isDarkMode = isDarkMode;
            await ApplyThemeAsync();
            await SaveThemeAsync();
            OnThemeChanged?.Invoke();
        }
    }

    private async Task ApplyThemeAsync()
    {
        try
        {
            if (_themeModule != null)
            {
                await _themeModule.InvokeVoidAsync("applyTheme", _isDarkMode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply theme");
        }
    }

    private async Task SaveThemeAsync()
    {
        try
        {
            if (_themeModule != null)
            {
                await _themeModule.InvokeVoidAsync("saveTheme", _isDarkMode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save theme preference");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_themeModule != null)
        {
            try
            {
                await _themeModule.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}
