namespace Battles.Grains.Abstractions;

/// <summary>
/// Boot probe for the co-hosted silo (ADR-0002): the smallest
/// possible grain call, used to prove that the silo is up and
/// routing. Carries no domain meaning.
/// </summary>
public interface IPingGrain : IGrainWithStringKey
{
    /// <summary>
    /// Echoes the key this grain was activated with.
    /// </summary>
    /// <returns>The grain's primary key.</returns>
    Task<string> PingAsync();
}
