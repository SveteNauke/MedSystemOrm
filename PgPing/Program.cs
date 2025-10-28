using System;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main()
    {
        var connString = Environment.GetEnvironmentVariable("MEDSYS_CONN");
        if (string.IsNullOrWhiteSpace(connString))
        {
            Console.WriteLine("MEDSYS_CONN nije postavljen.");
            return;
        }

        try
        {
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            Console.WriteLine("Uspješno spojeno na Supabase PostgreSQL");

            await using (var cmd = new NpgsqlCommand("select count(*) from pg_stat_activity", conn))
            {
                var obj = await cmd.ExecuteScalarAsync();
                long sessions = Convert.ToInt64(obj); 
                Console.WriteLine("Aktivne sesije: " + sessions);
            }

            await using (var cmd = new NpgsqlCommand("select count(*)::bigint from pg_stat_activity", conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    long sessions2 = reader.GetInt64(0);
                    Console.WriteLine("Aktivne sesije (BIGINT): " + sessions2);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Greška pri spajanju/izvršavanju:");
            Console.WriteLine(ex.Message);
        }
    }
}
