namespace OrleansMonsterArena.Tests;

/// <summary>
/// The container images of <c>compose.yaml</c>, read from the copy
/// in the test output. That file is the one place an image version
/// is pinned, so tests and local runs start the same images and a
/// version bump there is checked by the tests.
/// </summary>
internal static class ComposeImages
{
    private const string ImageKey = "image:";

    /// <summary>
    /// The image of the <c>azurite</c> service (Table Storage).
    /// </summary>
    public static string Azurite { get; } = ImageOf("azurite");

    /// <summary>
    /// The image of the <c>sqlserver</c> service.
    /// </summary>
    public static string SqlServer { get; } = ImageOf("sqlserver");

    // The "image:" line among the lines indented under the
    // service's own key ("  <service>:").
    private static string ImageOf(string service)
    {
        var image = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "compose.yaml"))
            .SkipWhile(line => line.TrimEnd() != $"  {service}:")
            .Skip(1)
            .TakeWhile(line => line.StartsWith("    ", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith(ImageKey, StringComparison.Ordinal));

        return image?[ImageKey.Length..].Trim()
            ?? throw new InvalidOperationException($"compose.yaml has no image for the service '{service}'.");
    }
}
