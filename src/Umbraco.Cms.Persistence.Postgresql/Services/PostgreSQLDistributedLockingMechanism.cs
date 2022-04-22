using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DistributedLocking;
using Umbraco.Cms.Core.DistributedLocking.Exceptions;
using Umbraco.Cms.Core.Exceptions;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

/// <summary>
/// SQL Server implementation of <see cref="IDistributedLockingMechanism"/>.
/// </summary>
public class PostgreSQLDistributedLockingMechanism : IDistributedLockingMechanism
{
    private readonly ILogger<PostgreSQLDistributedLockingMechanism> _logger;
    private readonly Lazy<IScopeAccessor> _scopeAccessor; // Hooray it's a circular dependency.
    private readonly IOptionsMonitor<GlobalSettings> _globalSettings;
    private readonly IOptionsMonitor<ConnectionStrings> _connectionStrings;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSQLDistributedLockingMechanism"/> class.
    /// </summary>
    public PostgreSQLDistributedLockingMechanism(
        ILogger<PostgreSQLDistributedLockingMechanism> logger,
        Lazy<IScopeAccessor> scopeAccessor,
        IOptionsMonitor<GlobalSettings> globalSettings,
        IOptionsMonitor<ConnectionStrings> connectionStrings)
    {
        _logger = logger;
        _scopeAccessor = scopeAccessor;
        _globalSettings = globalSettings;
        _connectionStrings = connectionStrings;
    }

    /// <inheritdoc />
    public bool Enabled => _connectionStrings.CurrentValue.IsConnectionStringConfigured() &&
                           _connectionStrings.CurrentValue.ProviderName == Constants.ProviderName;

    /// <inheritdoc />
    public IDistributedLock ReadLock(int lockId, TimeSpan? obtainLockTimeout = null)
    {
        obtainLockTimeout ??= _globalSettings.CurrentValue.DistributedLockingReadLockDefaultTimeout;
        return new PostgreSQLDistributedLock(this, lockId, DistributedLockType.ReadLock, obtainLockTimeout.Value);
    }

    /// <inheritdoc />
    public IDistributedLock WriteLock(int lockId, TimeSpan? obtainLockTimeout = null)
    {
        obtainLockTimeout ??= _globalSettings.CurrentValue.DistributedLockingWriteLockDefaultTimeout;
        return new PostgreSQLDistributedLock(this, lockId, DistributedLockType.WriteLock, obtainLockTimeout.Value);
    }

    private class PostgreSQLDistributedLock : IDistributedLock
    {
        private readonly PostgreSQLDistributedLockingMechanism _parent;
        private readonly TimeSpan _timeout;

        public PostgreSQLDistributedLock(
            PostgreSQLDistributedLockingMechanism parent,
            int lockId,
            DistributedLockType lockType,
            TimeSpan timeout)
        {
            _parent = parent;
            _timeout = timeout;
            LockId = lockId;
            LockType = lockType;

            _parent._logger.LogDebug("Requesting {lockType} for id {id}", LockType, LockId);

            try
            {
                switch (lockType)
                {
                    case DistributedLockType.ReadLock:
                        ObtainReadLock();
                        break;
                    case DistributedLockType.WriteLock:
                        ObtainWriteLock();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(lockType), lockType, @"Unsupported lockType");
                }
            }
            catch (SqlException ex) when (ex.Number == 1222)
            {
                if (LockType == DistributedLockType.ReadLock)
                {
                    throw new DistributedReadLockTimeoutException(LockId);
                }

                throw new DistributedWriteLockTimeoutException(LockId);
            }

            _parent._logger.LogDebug("Acquired {lockType} for id {id}", LockType, LockId);
        }

        public int LockId { get; }

        public DistributedLockType LockType { get; }

        public void Dispose()
        {
            // Mostly no op, cleaned up by completing transaction in scope.
            _parent._logger.LogDebug("Dropped {lockType} for id {id}", LockType, LockId);
        }

        public override string ToString()
            => $"PostgreSQLDistributedLock({LockId}, {LockType}";

        private void ObtainReadLock()
        {
            IUmbracoDatabase? db = _parent._scopeAccessor.Value.AmbientScope?.Database;

            if (db is null)
            {
                throw new PanicException("Could not find a database");
            }

            if (!db.InTransaction)
            {
                throw new InvalidOperationException("PostgreSQLDistributedLockingMechanism requires a transaction to function.");
            }

            if (db.Transaction.IsolationLevel < IsolationLevel.ReadCommitted)
            {
                throw new InvalidOperationException("A transaction with minimum ReadCommitted isolation level is required.");
            }

            const string query = "SELECT value FROM umbracoLock WITH (REPEATABLEREAD)  WHERE id=@id";

            db.Execute("SET LOCK_TIMEOUT " + _timeout.TotalMilliseconds + ";");

            var i = db.ExecuteScalar<int?>(query, new {id = LockId});

            if (i == null)
            {
                // ensure we are actually locking!
                throw new ArgumentException(@$"LockObject with id={LockId} does not exist.", nameof(LockId));
            }
        }

        private void ObtainWriteLock()
        {
            IUmbracoDatabase? db = _parent._scopeAccessor.Value.AmbientScope?.Database;

            if (db is null)
            {
                throw new PanicException("Could not find a database");
            }

            if (!db.InTransaction)
            {
                throw new InvalidOperationException("PostgreSQLDistributedLockingMechanism requires a transaction to function.");
            }

            if (db.Transaction.IsolationLevel < IsolationLevel.ReadCommitted)
            {
                throw new InvalidOperationException("A transaction with minimum ReadCommitted isolation level is required.");
            }

            const string query = @"UPDATE umbracoLock WITH (REPEATABLEREAD) SET value = (CASE WHEN (value=1) THEN -1 ELSE 1 END) WHERE id=@id";

            db.Execute("SET LOCK_TIMEOUT " + _timeout.TotalMilliseconds + ";");

            var i = db.Execute(query, new {id = LockId});

            if (i == 0)
            {
                // ensure we are actually locking!
                throw new ArgumentException($"LockObject with id={LockId} does not exist.");
            }
        }
    }
}
