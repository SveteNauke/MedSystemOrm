using System;


namespace MedSys.Orm
{
    public sealed class SchemaCreator
    {
        private readonly Npgsql.NpgsqlConnection _conn;
        private readonly DdlGenerator _ddlGen = new();

        public SchemaCreator(Npgsql.NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public async Task EnsureAsync(params Type[] entityTypes)
        {
            foreach (var et in entityTypes)
            {
                var em = EntityMeta.From(et);
                var sql = _ddlGen.CreateTableSql(em);

                Console.WriteLine(sql);

                await using var cmd = new Npgsql.NpgsqlCommand(sql, _conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }





}
