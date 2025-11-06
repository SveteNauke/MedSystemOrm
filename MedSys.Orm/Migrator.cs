using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace MedSys.Orm
{
    public sealed class Migrator
    {
        private readonly DbSession _session;
        private const string MigrationsTable = "__migrations";

        private static string Q(string ident) => PgTypeMapper.Quote(ident);

        public Migrator(DbSession session)
        {
            _session = session;
        }

        /// <summary>
        /// Osiguraj da tablica __migrations postoji.
        /// </summary>
        private async Task EnsureMigrationsTableAsync()
        {
            await _session.OpenAsync();

            var sql = $@"
                CREATE TABLE IF NOT EXISTS {Q(MigrationsTable)} (
                    id VARCHAR(100) PRIMARY KEY,
                    applied_at TIMESTAMP NOT NULL DEFAULT NOW()
                );
            ";

            await using var cmd = new NpgsqlCommand(sql, _session.Connection);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Dohvati id-ove već primijenjenih migracija.
        /// </summary>
        private async Task<HashSet<string>> GetAppliedIdsAsync()
        {
            await EnsureMigrationsTableAsync();

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var sql = $"SELECT id FROM {Q(MigrationsTable)};";
            await using var cmd = new NpgsqlCommand(sql, _session.Connection);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        /// <summary>
        /// Primijeni sve migracije koje još nisu primijenjene (REDOSLIJED po kolekciji).
        /// </summary>
        public async Task ApplyAsync(IEnumerable<IMigration> migrations)
        {
            await _session.OpenAsync();
            var applied = await GetAppliedIdsAsync();

            foreach (var m in migrations)
            {
                if (applied.Contains(m.Id))
                {
                    Console.WriteLine($"[Migrator] Preskačem već primijenjenu migraciju {m.Id}");
                    continue;
                }

                Console.WriteLine($"[Migrator] Primjenjujem migraciju {m.Id}...");

                await using var tx = await _session.Connection.BeginTransactionAsync();
                try
                {
                    await using (var cmd = new NpgsqlCommand(m.UpSql, _session.Connection, tx))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    var insertSql = $"INSERT INTO {Q(MigrationsTable)} (id) VALUES (@id);";
                    await using (var cmd = new NpgsqlCommand(insertSql, _session.Connection, tx))
                    {
                        cmd.Parameters.AddWithValue("id", m.Id);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    Console.WriteLine($"[Migrator] Migracija {m.Id} uspješno primijenjena.");
                }
                catch
                {
                    await tx.RollbackAsync();
                    Console.WriteLine($"[Migrator] Greška pri migraciji {m.Id}, rollback.");
                    throw;
                }
            }
        }

        /// <summary>
        /// Rollback konkretne migracije (pomoću njezinog DownSql-a).
        /// </summary>
        public async Task RollbackAsync(IMigration migration)
        {
            await _session.OpenAsync();
            await EnsureMigrationsTableAsync();

            Console.WriteLine($"[Migrator] Rollback migracije {migration.Id}...");

            await using var tx = await _session.Connection.BeginTransactionAsync();
            try
            {
                await using (var cmd = new NpgsqlCommand(migration.DownSql, _session.Connection, tx))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                var deleteSql = $"DELETE FROM {Q(MigrationsTable)} WHERE id = @id;";
                await using (var cmd = new NpgsqlCommand(deleteSql, _session.Connection, tx))
                {
                    cmd.Parameters.AddWithValue("id", migration.Id);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                Console.WriteLine($"[Migrator] Migracija {migration.Id} rollback-ana.");
            }
            catch
            {
                await tx.RollbackAsync();
                Console.WriteLine($"[Migrator] Greška pri rollbacku {migration.Id}, rollback transakcije.");
                throw;
            }
        }
    }
}
