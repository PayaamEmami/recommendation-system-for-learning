using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rsl.Api.Configuration;
using Rsl.Api.Extensions;

namespace Rsl.Tests.Unit.Api;

[TestClass]
public sealed class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddJwtAuthentication_WhenMissingSettings_Throws()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        TestAssert.Throws<InvalidOperationException>(() =>
            services.AddJwtAuthentication(configuration));
    }

    [TestMethod]
    public void AddJwtAuthentication_RegistersJwtSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-for-unit-tests-only",
                ["JwtSettings:Issuer"] = "issuer",
                ["JwtSettings:Audience"] = "audience"
            })
            .Build();

        services.AddJwtAuthentication(configuration);
        var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<JwtSettings>();
        Assert.AreEqual("issuer", settings.Issuer);
        Assert.AreEqual("audience", settings.Audience);
    }

    [TestMethod]
    public void AddRegistrationSettings_RegistersSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Registration:Enabled"] = "true",
                ["Registration:DisabledMessage"] = "off"
            })
            .Build();

        services.AddRegistrationSettings(configuration);
        var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<RegistrationSettings>();
        Assert.IsTrue(settings.Enabled);
        Assert.AreEqual("off", settings.DisabledMessage);
    }

    [TestMethod]
    public void AddApiVersioningConfiguration_ConfiguresDefaults()
    {
        var services = new ServiceCollection();

        services.AddApiVersioningConfiguration();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;
        Assert.AreEqual(new ApiVersion(1, 0), options.DefaultApiVersion);
        Assert.IsTrue(options.AssumeDefaultVersionWhenUnspecified);
    }

    [TestMethod]
    public void AddRateLimitingConfiguration_SetsRejectionStatusCode()
    {
        var services = new ServiceCollection();

        services.AddRateLimitingConfiguration();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        Assert.AreEqual(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);
    }
}
