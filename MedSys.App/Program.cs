using System;
using System.Data;
using System.Threading.Tasks;
using MedSys.Domain;
using MedSys.Orm;
using Npgsql;

internal class Program
{
    private static async Task Main()
    {
        var cs = Environment.GetEnvironmentVariable("MEDSYS_CONN")
                 ?? throw new InvalidOperationException("MEDSYS_CONN nije postavljen.");

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();
        Console.WriteLine("Uspješno spojeno na Supabase PostgreSQL");

        var meta = EntityMeta.From<Patient>();
        var ddlGen = new DdlGenerator();
        var ddlSql = ddlGen.CreateTableSql(meta);

        Console.WriteLine("-------DDL PATIENTS-------");
        Console.WriteLine(ddlSql);

        await using (var ddlCmd = new NpgsqlCommand(ddlSql, conn))
        {
            await ddlCmd.ExecuteNonQueryAsync();
        }
        Console.WriteLine("Tabela 'patients' je kreirana ili već postoji.");

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        const string insertSql = @"
            INSERT INTO patients (fname, lname, birth_date)
            VALUES (@f, @l, @b)
            RETURNING id;
        ";

        await using var insertCmd = new NpgsqlCommand(insertSql, conn);
        insertCmd.Parameters.AddWithValue("f", "Ana");
        insertCmd.Parameters.AddWithValue("l", "Kovač");
        insertCmd.Parameters.AddWithValue("b", new DateTime(1990, 2, 1));

        var newIdObj = await insertCmd.ExecuteScalarAsync();
        if (newIdObj == null)
            throw new Exception("INSERT nije vratio ID.");

        var newId = Convert.ToInt32(newIdObj);
        Console.WriteLine($"Ubacen pacijent s ID = {newId}");

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        const string selectSql = @"
            SELECT id, fname, lname, birth_date
            FROM patients
            WHERE id = @id;
        ";

        await using var selectCmd = new NpgsqlCommand(selectSql, conn);
        selectCmd.Parameters.AddWithValue("id", newId);

        await using var reader = await selectCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var id = reader.GetInt32(reader.GetOrdinal("id"));
            var fname = reader.GetString(reader.GetOrdinal("fname"));
            var lname = reader.GetString(reader.GetOrdinal("lname"));

            DateTime? birth = reader.IsDBNull(reader.GetOrdinal("birth_date"))
                ? (DateTime?)null
                : reader.GetDateTime(reader.GetOrdinal("birth_date"));

            var birthText = birth?.ToString("yyyy-MM-dd") ?? "N/A";

            Console.WriteLine("=== Pacijent iz baze ===");
            Console.WriteLine($"ID:      {id}");
            Console.WriteLine($"Ime:     {fname}");
            Console.WriteLine($"Prezime: {lname}");
            Console.WriteLine($"Rođen:   {birthText}");
        }
        else
        {
            Console.WriteLine("❌ Nije pronađen pacijent s tim ID-em.");
        }
    }
}
