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

        }
        catch (Exception ex)
        {
            Console.WriteLine("Greška pri spajanju/izvršavanju:");
            Console.WriteLine(ex.Message);
        }
    }
}
