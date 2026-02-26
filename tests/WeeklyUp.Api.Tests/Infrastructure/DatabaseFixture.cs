using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Respawn;

using WeeklyUp.Infrastructure.Persistence;

namespace WeeklyUp.Api.Tests.Infrastructure;

/// <summary>
/// Fixture compartilhada por todos os testes E2E.
/// Cria o banco de testes uma vez e usa Respawn para limpar entre testes.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime, IDisposable
{
    private readonly WeeklyUpWebAppFactory _factory = new();
    private Respawner _respawner = null!;

    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        Client = _factory.CreateClient();

        // Garante que o schema existe no banco de testes
        using IServiceScope scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        // Configura Respawn para limpar tabelas entre testes
        await using var conn = new NpgsqlConnection(WeeklyUpWebAppFactory.ConnectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(WeeklyUpWebAppFactory.ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public void Dispose() => Client.Dispose();

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
