using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Players.Contracts;
using Players.Features.GetPlayerStats;
using Shared.Infrastructure.Persistence;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Plays the player stats end to end: results reported through
/// <see cref="IPlayerStatsService"/> land in SQL Server and show up
/// on <c>GET /players/{playerId}</c>. Every test uses fresh player
/// ids, since the host's database is shared.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class PlayerStatsTests(AppHostFixture host)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private IPlayerStatsService Stats => host.Factory.Services.GetRequiredService<IPlayerStatsService>();

    [Fact]
    public async Task RecordResultAsync_FirstBattle_CountsWinAndLoss()
    {
        // Arrange
        var winner = NewPlayerId();
        var loser = NewPlayerId();
        using var client = host.Factory.CreateClient();

        // Act
        await Stats.RecordResultAsync(new BattleResultDto(winner, [loser]), Cancellation);

        // Assert
        Assert.Equal(new PlayerStatsResponse(winner, 1, 0), await GetStatsAsync(client, winner));
        Assert.Equal(new PlayerStatsResponse(loser, 0, 1), await GetStatsAsync(client, loser));
    }

    [Fact]
    public async Task RecordResultAsync_SeveralBattles_AddsUp()
    {
        // Arrange
        var player = NewPlayerId();
        var rival = NewPlayerId();
        using var client = host.Factory.CreateClient();

        // Act
        await Stats.RecordResultAsync(new BattleResultDto(player, [rival]), Cancellation);
        await Stats.RecordResultAsync(new BattleResultDto(player, [rival]), Cancellation);
        await Stats.RecordResultAsync(new BattleResultDto(rival, [player]), Cancellation);

        // Assert
        Assert.Equal(new PlayerStatsResponse(player, 2, 1), await GetStatsAsync(client, player));
    }

    [Fact]
    public async Task RecordResultAsync_ConcurrentFirstBattles_LosesNoCount()
    {
        // Arrange
        const int battles = 20;
        var player = NewPlayerId();
        using var client = host.Factory.CreateClient();

        // Act
        await Task.WhenAll(Enumerable.Range(0, battles).Select(_ =>
            Stats.RecordResultAsync(new BattleResultDto(player, [NewPlayerId()]), Cancellation)));

        // Assert
        Assert.Equal(new PlayerStatsResponse(player, battles, 0), await GetStatsAsync(client, player));
    }

    [Theory]
    [InlineData("", "loser")]
    [InlineData("winner", "")]
    [InlineData("same", "same")]
    [InlineData("this-player-id-has-sixty-five-characters-one-more-than-allowed-64", "loser")]
    public async Task RecordResultAsync_InvalidSides_ThrowsArgumentException(string winner, string loser)
    {
        // Arrange
        var result = new BattleResultDto(winner, [loser]);

        // Act
        var record = () => Stats.RecordResultAsync(result, Cancellation);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(record);
    }

    [Fact]
    public async Task RecordResultAsync_PlayerBeatsBot_CountsWinOnly()
    {
        // Arrange
        var player = NewPlayerId();
        using var client = host.Factory.CreateClient();

        // Act
        await Stats.RecordResultAsync(new BattleResultDto(player, []), Cancellation);

        // Assert
        Assert.Equal(new PlayerStatsResponse(player, 1, 0), await GetStatsAsync(client, player));
    }

    [Fact]
    public async Task RecordResultAsync_BotBeatsPlayer_CountsLossOnly()
    {
        // Arrange
        var player = NewPlayerId();
        using var client = host.Factory.CreateClient();

        // Act
        await Stats.RecordResultAsync(new BattleResultDto(null, [player]), Cancellation);

        // Assert
        Assert.Equal(new PlayerStatsResponse(player, 0, 1), await GetStatsAsync(client, player));
    }

    [Fact]
    public async Task RecordResultAsync_NoPlayer_ThrowsArgumentException()
    {
        // Arrange
        var result = new BattleResultDto(null, []);

        // Act
        var record = () => Stats.RecordResultAsync(result, Cancellation);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(record);
    }

    [Fact]
    public async Task GetPlayer_NoDecidedBattle_Returns404()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.GetAsync(PlayerUri(NewPlayerId()), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Migrate_PlayersModule_KeepsTablesAndHistoryInOwnSchema()
    {
        // Arrange
        var connectionString = host.Factory.Services
            .GetRequiredService<IConfiguration>()
            .GetConnectionString(ModuleDatabaseExtensions.ConnectionName);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(Cancellation);
        await using var command = new SqlCommand(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'players'",
            connection);

        // Act
        List<string> tables = [];
        await using var reader = await command.ExecuteReaderAsync(Cancellation);
        while (await reader.ReadAsync(Cancellation))
        {
            tables.Add(reader.GetString(0));
        }

        // Assert
        Assert.Equal(["PlayerStats", "__EFMigrationsHistory"], tables.Order(StringComparer.Ordinal));
    }

    private static string NewPlayerId() => $"player-{Guid.NewGuid():N}";

    private static Uri PlayerUri(string playerId) => new($"/players/{playerId}", UriKind.Relative);

    private static Task<PlayerStatsResponse?> GetStatsAsync(HttpClient client, string playerId) =>
        client.GetFromJsonAsync<PlayerStatsResponse>(PlayerUri(playerId), Cancellation);
}
