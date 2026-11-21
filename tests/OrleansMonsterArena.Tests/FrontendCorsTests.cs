using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The browser reaches the API from the frontend's origin
/// (ADR-0004): that origin is admitted, any other is not.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class FrontendCorsTests(AppHostFixture host)
{
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";

    private static readonly Uri Arenas = new("/arenas", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetArenas_FromTheFrontendOrigin_AllowsThatOrigin()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Arenas);
        request.Headers.Add("Origin", AppHostFixture.FrontendOrigin);

        // Act
        using var response = await client.SendAsync(request, Cancellation);

        // Assert
        Assert.Equal([AppHostFixture.FrontendOrigin], response.Headers.GetValues(AllowOriginHeader));
    }

    [Fact]
    public async Task GetArenas_FromAnotherOrigin_AllowsNothing()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Arenas);
        request.Headers.Add("Origin", "http://elsewhere.test");

        // Act
        using var response = await client.SendAsync(request, Cancellation);

        // Assert
        Assert.False(response.Headers.Contains(AllowOriginHeader));
    }

    [Fact]
    public async Task PreflightForStartingABattle_FromTheFrontendOrigin_AllowsThePost()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Options, new Uri($"/arenas/{TestArenas.Volcano}/battles", UriKind.Relative));
        request.Headers.Add("Origin", AppHostFixture.FrontendOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        // Act
        using var response = await client.SendAsync(request, Cancellation);

        // Assert
        Assert.Equal([AppHostFixture.FrontendOrigin], response.Headers.GetValues(AllowOriginHeader));
        Assert.Equal(["POST"], response.Headers.GetValues("Access-Control-Allow-Methods"));
    }
}
