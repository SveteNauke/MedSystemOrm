using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace MedSys.Orm.Migrations;

public static class Migrator
{
    private const string MigrationTable = "__migrations";

    public static async Task MigrateUpAsync(DbSession session)
    {
        await EnsureMigrationsTableAsync(session);

        var applied = await GetAppliedMigrationIdsAsync(session);

        var migrationTypes = typeof(IMigration).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IMigration).IsAssignableFrom(t))
            .ToList();

        var migrations = migrationTypes
            .Select(t => (IMigration)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Id)
            .ToList();

        foreach (var m in migrations)
        {
            if (applied.Contains(m.Id))
                continue;

            Console.WriteLine($"[MIGRATE UP] {m.Id} – {m.Name}");
            await m.UpAsync(session);
            await InsertMigrationRowAsync(session, m.Id, m.Name);
        }

        Console.WriteLine("Sve nove migracije su izvršene.");
    }

    public static async Task MigrateDownLastAsync(DbSession session)
    {
        await EnsureMigrationsTableAsync(session);

        var applied = await GetAppliedMigrationsAsync(session);
        if (applied.Count == 0)
        {
            Console.WriteLine("Nema primijenjenih migracija za rollback.");
            return;
        }

        var last = applied.OrderByDescending(m => m.appliedAt).First();

        var migrationType = typeof(IMigration).Assembly
            .GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract &&
                typeof(IMigration).IsAssignableFrom(t) &&
                ((IMigration)Activator.CreateInstance(t)!).Id == last.id);

        if (migrationType == null)
        {
            Console.WriteLine($"Upozorenje: ne mogu pronaći klasu za migraciju '{last.id}'.");
            return;
        }

        var migration = (IMigration)Activator.CreateInstance(migrationType)!;

        Console.WriteLine($"[MIGRATE DOWN] {migration.Id} – {migration.Name}");
        await migration.DownAsync(session);
        await DeleteMigrationRowAsync(session, migration.Id);

        Console.WriteLine("Rollback zadnje migracije završen.");
    }

 

    private static async Task EnsureMigrationsTableAsync(DbSession session)
    {
        await session.OpenAsync();

        var sql = $@"
            CREATE TABLE IF NOT EXISTS {MigrationTable} (
                id          VARCHAR(100) PRIMARY KEY,
                name        VARCHAR(255) NOT NULL,
                applied_at  TIMESTAMP    NOT NULL DEFAULT NOW()
            );
        ";

        await using var cmd = new NpgsqlCommand(sql, session.Connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<HashSet<string>> GetAppliedMigrationIdsAsync(DbSession session)
    {
        await session.OpenAsync();

        var set = new HashSet<string>();

        var sql = $"SELECT id FROM {MigrationTable};";
        await using var cmd = new NpgsqlCommand(sql, session.Connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            set.Add(reader.GetString(0));
        }

        return set;
    }

    private static async Task<List<(string id, DateTime appliedAt)>> GetAppliedMigrationsAsync(DbSession session)
    {
        await session.OpenAsync();

        var list = new List<(string id, DateTime appliedAt)>();

        var sql = $"SELECT id, applied_at FROM {MigrationTable};";
        await using var cmd = new NpgsqlCommand(sql, session.Connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var id  = reader.GetString(0);
            var at  = reader.GetDateTime(1);
            list.Add((id, at));
        }

        return list;
    }

    private static async Task InsertMigrationRowAsync(DbSession session, string id, string name)
    {
        await session.OpenAsync();

        var sql = $@"
            INSERT INTO {MigrationTable} (id, name, applied_at)
            VALUES (@id, @name, NOW());
        ";

        await using var cmd = new NpgsqlCommand(sql, session.Connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", name);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task DeleteMigrationRowAsync(DbSession session, string id)
    {
        await session.OpenAsync();

        var sql = $@"DELETE FROM {MigrationTable} WHERE id = @id;";
        await using var cmd = new NpgsqlCommand(sql, session.Connection);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
