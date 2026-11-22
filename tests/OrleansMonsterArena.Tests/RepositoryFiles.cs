namespace OrleansMonsterArena.Tests;

/// <summary>
/// Finds files of the repository from a test. The tests run from
/// <c>bin/</c>, so the repository root is the nearest parent that
/// holds the solution file.
/// </summary>
internal static class RepositoryFiles
{
    /// <summary>
    /// The full path of a file, given its parts below the root.
    /// </summary>
    /// <param name="parts">The path below the repository root.</param>
    /// <returns>The full path.</returns>
    public static string PathTo(params string[] parts) => Path.Combine([Root(), .. parts]);

    private static string Root()
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
