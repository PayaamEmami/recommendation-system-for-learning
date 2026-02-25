using Rsl.Core.Enums;
using Rsl.Jobs.Validation;

namespace Rsl.Tests.Unit.Jobs;

[TestClass]
public sealed class ResourceUrlPolicyTests
{
    [DataTestMethod]
    [DataRow("https://www.deeplearning.ai/the-batch/tag/jan-30-2026/")]
    [DataRow("https://example.com/category/machine-learning/")]
    [DataRow("https://example.com/2026/01/30/")]
    [DataRow("https://example.com/blog?page=2")]
    public void IsLikelyResourceUrl_ForBlogPost_NonContentPagesAreRejected(string url)
    {
        var result = ResourceUrlPolicy.IsLikelyResourceUrl(url, ResourceType.BlogPost, "https://example.com");

        Assert.IsFalse(result);
    }

    [DataTestMethod]
    [DataRow("https://www.deeplearning.ai/the-batch/some-actual-post/")]
    [DataRow("https://example.com/posts/how-transformers-work")]
    [DataRow("https://example.com/2026/01/30/post-title")]
    public void IsLikelyResourceUrl_ForBlogPost_RealPostsAreAccepted(string url)
    {
        var result = ResourceUrlPolicy.IsLikelyResourceUrl(url, ResourceType.BlogPost, "https://example.com");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsLikelyResourceUrl_SourceRootUrlIsRejected()
    {
        var result = ResourceUrlPolicy.IsLikelyResourceUrl(
            "https://example.com/feed",
            ResourceType.BlogPost,
            "https://example.com/feed");

        Assert.IsFalse(result);
    }
}
