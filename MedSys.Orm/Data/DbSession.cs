using System;
using System.Threading.Tasks;
using Npgsql; 
using System.Data;
namespace MedSys.Orm
{
    public sealed class DbSession : IAsyncDisposable
    {
        public NpgsqlConnection Connection { get; }
        public NpgsqlTransaction? Transaction { get; private set; }

        public DbSession(string connString)
        {
            Connection = new NpgsqlConnection(connString);
        }

        public async Task OpenAsync()
        {
            if (Connection.State != ConnectionState.Open)
                await Connection.OpenAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (Transaction != null)
                throw new InvalidOperationException("Transakcija je već započeta.");

            await OpenAsync();
            Transaction = await Connection.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (Transaction == null)
                throw new InvalidOperationException("Nema započete transakcije za commit.");

            await Transaction.CommitAsync();
            await Transaction.DisposeAsync();
            Transaction = null;
        }

        public async Task RollbackTransactionAsync()
        {
            if (Transaction == null)
                throw new InvalidOperationException("Nema započete transakcije za rollback.");

            await Transaction.RollbackAsync();
            await Transaction.DisposeAsync();
            Transaction = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (Transaction != null)
            {
                await Transaction.DisposeAsync();
                Transaction = null;
            }

            await Connection.DisposeAsync();
        }
    }
}