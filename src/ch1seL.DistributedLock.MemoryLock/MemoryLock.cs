using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Distributed
{
    public class MemoryLock : IDistributedLock, IDisposable
    {
        public static readonly string SemaphoreWrapperTypeFullName = typeof(SemaphoreWrapper).FullName;
        private readonly int[] _keyLocks;
        private readonly object _lockWrapperMap = new object();

        public MemoryLock()
        {
            var lockCount = Math.Max(Environment.ProcessorCount * 8, 32);
            _keyLocks = new int[lockCount];
        }

        public void Dispose()
        {
        }

        public async Task<IDisposable> CreateLockAsync(string resource, TimeSpan? expiryTime = null, TimeSpan? waitTime = null, TimeSpan? retryTime = null,
            CancellationToken cancellationToken = default)
        {
            var hash = (uint) resource.GetHashCode() % (uint) _keyLocks.Length;
            while (Interlocked.CompareExchange(ref _keyLocks[hash], 1, 0) == 1) Thread.Yield();

            try
            {
                return await new SemaphoreWrapper(resource).WaitAsync(waitTime ?? TimeSpan.FromSeconds(30), cancellationToken);
            }
            finally
            {
                _keyLocks[hash] = 0;
            }
        }

        private class SemaphoreWrapper : IDisposable
        {
            private readonly string _resource;
            private readonly SemaphoreSlim _semaphoreSlim;

            public SemaphoreWrapper(string resource)
            {
                _resource = resource;
                _semaphoreSlim = new SemaphoreSlim(1, 1);
            }

            public void Dispose()
            {
                _semaphoreSlim.Release();
                _semaphoreSlim.Dispose();
            }

            public async Task<IDisposable> WaitAsync(TimeSpan waitTime, CancellationToken cancellationToken)
            {
                Exception innerException = null;

                await Task.Delay(10, cancellationToken);
                var waitResultTask = _semaphoreSlim.WaitAsync(waitTime, cancellationToken);

                try
                {
                    var waitResult = await waitResultTask;
                    if (waitResult) return this;
                }
                catch (Exception exception)
                {
                    innerException = exception;
                }

                throw new DistributedLockException(_resource, null, DistributedLockBadStatus.Conflicted, innerException);
            }
        }
    }
}