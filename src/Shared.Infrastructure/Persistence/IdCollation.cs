namespace Shared.Infrastructure.Persistence;

/// <summary>
/// The SQL collation of every id column. Ids are compared exactly
/// as written everywhere else — as grain keys and in code — so SQL
/// Server must not treat <c>Volcano</c> and <c>volcano</c> as the
/// same id, which its default collation does.
/// </summary>
public static class IdCollation
{
    /// <summary>
    /// A binary collation: two ids are equal only if every character
    /// is. SQL Server still ignores trailing spaces when it compares,
    /// so lookups by id check the match in code as well.
    /// </summary>
    public const string Ordinal = "Latin1_General_100_BIN2";
}
