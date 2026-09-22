using Battles.Grains.Abstractions;

namespace Battles.Grains;

/// <summary>
/// Implementation of the <see cref="IPingGrain"/> boot probe.
/// </summary>
public sealed class PingGrain : Grain, IPingGrain
{
    /// <inheritdoc />
    public Task<string> PingAsync() => Task.FromResult(this.GetPrimaryKeyString());
}
