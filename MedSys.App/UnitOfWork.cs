using System;
using System.Threading.Tasks;
using MedSys.Domain;

namespace MedSys.Orm
{
    /// <summary>
    /// Unit of Work – centralizira rad s više repozitorija unutar jedne transakcije.
    /// Koristi postojeći DbSession (dijeli konekciju i transakciju).
    /// </summary>
    public sealed class UnitOfWork : IAsyncDisposable
    {
        private readonly DbSession _session;
        private bool _transactionStarted;

        public Repository<Patient> Patients { get; }
        public Repository<Visit> Visits { get; }
        public Repository<Medicine> Medicines { get; }
        public Repository<Prescription> Prescriptions { get; }

        public UnitOfWork(DbSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            
            Patients      = new Repository<Patient>(_session);
            Visits        = new Repository<Visit>(_session);
            Medicines     = new Repository<Medicine>(_session);
            Prescriptions = new Repository<Prescription>(_session);
        }

        public async Task BeginAsync()
        {
            if (_transactionStarted)
                throw new InvalidOperationException("Transakcija je već započeta u ovom UnitOfWork-u.");

            await _session.BeginTransactionAsync();
            _transactionStarted = true;
        }

        public async Task CommitAsync()
        {
            if (!_transactionStarted)
                throw new InvalidOperationException("Nema aktivne transakcije za commit.");

            await _session.CommitTransactionAsync();
            _transactionStarted = false;
        }

        public async Task RollbackAsync()
        {
            if (!_transactionStarted)
                return;

            await _session.RollbackTransactionAsync();
            _transactionStarted = false;
        }

        public async ValueTask DisposeAsync()
        {
            if (_transactionStarted)
            {
                await _session.RollbackTransactionAsync();
                _transactionStarted = false;
            }
        }
    }
}
