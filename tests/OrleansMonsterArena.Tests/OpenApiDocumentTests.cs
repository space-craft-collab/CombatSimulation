using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The OpenAPI document the host serves is committed as
/// <c>docs/api/openapi.json</c>, and the frontend's Kiota client is
/// generated from that file (ADR-0004). This test keeps the two in
/// step: an API change fails here until the file is refreshed.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class OpenApiDocumentTests(AppHostFixture host)
{
    /// <summary>
    /// Set to <c>1</c> to write the served document over the
    /// committed one instead of comparing them.
    /// </summary>
    private const string UpdateVariable = "UPDATE_OPENAPI";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetOpenApiDocument_CurrentEndpoints_MatchesTheCommittedDocument()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var path = Path.Combine(RepositoryRoot(), "docs", "api", "openapi.json");

        // Act
        var served = Normalize(await client.GetStringAsync(
            new Uri("/openapi/v1.json", UriKind.Relative), Cancellation));

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(path, served, Cancellation);
        }

        // Assert
        var committed = Normalize(await File.ReadAllTextAsync(path, Cancellation));

        if (served != committed)
        {
            Assert.Fail(
                $"The served OpenAPI document differs from docs/api/openapi.json, first at line "
                + $"{FirstDifferingLine(served, committed)}. Rerun the tests with {UpdateVariable}=1 "
                + "and regenerate the frontend client (see CONTRIBUTING.md).");
        }
    }

    // One trailing newline and LF line endings, whatever the
    // server or the checkout produced.
    private static string Normalize(string document) =>
        document.ReplaceLineEndings("\n").TrimEnd() + "\n";

    private static int FirstDifferingLine(string served, string committed)
    {
        var servedLines = served.Split('\n');
        var committedLines = committed.Split('\n');
        var shared = Math.Min(servedLines.Length, committedLines.Length);

        for (var line = 0; line < shared; line++)
        {
            if (servedLines[line] != committedLines[line])
            {
                return line + 1;
            }
        }

        return shared + 1;
    }

    // The tests run from bin/, so the repository root is the
    // nearest parent that holds the solution file.
    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OrleansMonsterArena.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("No OrleansMonsterArena.slnx above the test output directory.");
    }
}
