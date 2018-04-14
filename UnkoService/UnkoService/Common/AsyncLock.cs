using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnkoService
{
    public sealed class AsyncLock
    {
        private readonly Task<IDisposable> releaser;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public AsyncLock()
        {
            this.releaser = Task.FromResult((IDisposable) new Releaser(this));
        }

        public Task<IDisposable> LockAsync()
        {
            var wait = this.semaphore.WaitAsync();
            return wait.IsCompleted
                ? this.releaser
                : wait.ContinueWith(
                    (_, state) => (IDisposable) state,
                    this.releaser.Result,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                );
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock toRelease;

            internal Releaser(AsyncLock toRelease)
            {
                this.toRelease = toRelease;
            }

            public void Dispose()
            {
                this.toRelease.semaphore.Release();
            }
        }
    }
}