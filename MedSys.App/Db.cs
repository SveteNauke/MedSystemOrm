using Npgsql;
using System;

namespace MedSys.App;

public static class Db
{
    public static async Task ExecAsync(string sql, params (string, object?)[] p)
    {
        var cs = Environment.GetEnvironmentVariable("MEDSYS_CONN")
                ?? throw new InvalidOperationException("Connection string nije dobro postavljen");
        await using var c = new NpgsqlConnection(cs);
        await c.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, c);

        foreach (var (n, v) in p)
            cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }    
    
    public static async Task<T?> ScalarAsync<T>(string sql, params (string, object?)[] p)
    {
        var cs = Environment.GetEnvironmentVariable("MEDSYS_CONN")
            ?? throw new InvalidOperationException("MEDSYS_CONN nije dobro postavljen");

        await using var c = new NpgsqlConnection(cs);
        await c.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, c);
        foreach (var (n, v) in p)
            cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);

        var obj = await cmd.ExecuteScalarAsync();
        return obj is null or DBNull ? default : (T)Convert.ChangeType(obj, typeof(T))!;
    }
}