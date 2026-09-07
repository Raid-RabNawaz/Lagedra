using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lagedra.Tests.Integration;

/// <summary>
/// A real PostgreSQL instance for tests that exercise EF Core against the
/// actual provider. These cannot run on the in-memory provider: the defects
/// they guard against (INSERT emitted as UPDATE, zero-row concurrency
/// failures) only surface on a relational database.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("lagedra_tests")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

public static class PostgresSchema
{
    /// <summary>
    /// Migrates a module's schema and empties every table in it, so each test
    /// starts from a known state without hardcoding a table list that drifts
    /// as migrations are added.
    /// </summary>
    public static async Task MigrateAndClearAsync(this DbContext db, string schema)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        // Interpolated rather than parameterised: a DO block body is an opaque
        // string literal to the server, so it cannot carry bind parameters.
        // Callers pass compile-time constants, and this guard keeps it that way.
        if (!System.Text.RegularExpressions.Regex.IsMatch(schema, "^[a-z_]+$"))
        {
            throw new ArgumentException($"Unexpected schema name '{schema}'.", nameof(schema));
        }

        await db.Database.MigrateAsync();

        // Interpolation is deliberate and the schema is validated above.
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            $"""
            DO $$
            DECLARE target text;
            BEGIN
                FOR target IN
                    SELECT tablename FROM pg_tables
                    WHERE schemaname = '{schema}'
                      AND tablename <> '__EFMigrationsHistory'
                LOOP
                    EXECUTE format('TRUNCATE TABLE %I.%I CASCADE', '{schema}', target);
                END LOOP;
            END $$;
            """);
#pragma warning restore EF1002
    }
}
