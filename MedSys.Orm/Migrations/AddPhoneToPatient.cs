using System.Threading.Tasks;
using Npgsql;

namespace MedSys.Orm.Migrations;

public sealed class AddPhoneToPatient : IMigration
{
    public string Id   => "20241106_AddPhoneToPatient";
    public string Name => "Add phone column to patients";

    public async Task UpAsync(DbSession session)
    {
        await session.OpenAsync();

        var sql = @"
            ALTER TABLE IF EXISTS patients
            ADD COLUMN IF NOT EXISTS phone VARCHAR(20);
        ";

        await using var cmd = new NpgsqlCommand(sql, session.Connection);
        if (session.Transaction != null)
            cmd.Transaction = session.Transaction;

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DownAsync(DbSession session)
    {
        await session.OpenAsync();

        var sql = @"
            ALTER TABLE IF EXISTS patients
            DROP COLUMN IF EXISTS phone;
        ";

        await using var cmd = new NpgsqlCommand(sql, session.Connection);
        if (session.Transaction != null)
            cmd.Transaction = session.Transaction;

        await cmd.ExecuteNonQueryAsync();
    }
}
