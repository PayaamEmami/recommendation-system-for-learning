using Rsl.Web.Services;

namespace Rsl.Tests.Unit.Web;

[TestClass]
public sealed class ThemeServiceTests
{
    [TestMethod]
    public async Task InitializeAsync_AppliesDefaultDarkTheme()
    {
        var jsRuntime = new TestJsRuntime();
        var service = new ThemeService(jsRuntime);

        await service.InitializeAsync();

        Assert.HasCount(1, jsRuntime.Calls);
        Assert.AreEqual("eval", jsRuntime.Calls[0].Identifier);
#pragma warning disable MSTEST0037
        Assert.IsTrue(jsRuntime.Calls[0].Args?[0]?.ToString()?.Contains("dark-mode", StringComparison.Ordinal) == true);
#pragma warning restore MSTEST0037
    }

    [TestMethod]
    public async Task ToggleThemeAsync_FlipsModeAndApplies()
    {
        var jsRuntime = new TestJsRuntime();
        var service = new ThemeService(jsRuntime);

        await service.ToggleThemeAsync();

        Assert.IsFalse(service.IsDarkMode);
        Assert.HasCount(1, jsRuntime.Calls);
#pragma warning disable MSTEST0037
        Assert.IsTrue(jsRuntime.Calls[0].Args?[0]?.ToString()?.Contains("light-mode", StringComparison.Ordinal) == true);
#pragma warning restore MSTEST0037
    }
}
