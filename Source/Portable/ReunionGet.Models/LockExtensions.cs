using System;
using System.Threading;

namespace ReunionGet.Models
{
    public static class LockExtensions
    {
        public readonly struct ReadLockHolder : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public ReadLockHolder(ReaderWriterLockSlim @lock)
                => _lock = @lock;

            public void Dispose() => _lock.ExitReadLock();
        }

        public static ReadLockHolder UseReadLock(this ReaderWriterLockSlim @lock)
        {
            @lock.EnterReadLock();
            return new ReadLockHolder(@lock);
        }

        public readonly struct WriteLockHolder : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public WriteLockHolder(ReaderWriterLockSlim @lock)
                => _lock = @lock;

            public void Dispose() => _lock.ExitWriteLock();
        }

        public static WriteLockHolder UseWriteLock(this ReaderWriterLockSlim @lock)
        {
            @lock.EnterWriteLock();
            return new WriteLockHolder(@lock);
        }
    }
}
