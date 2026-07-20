using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRMS.Application.Interfaces
{
    /// <summary>
    /// "Cổng khoá" dùng chung giữa các service cùng thao tác trên 1 IApplicationDbContext (Scoped)
    /// trong Blazor Server. Vì DbContext không thread-safe, nếu 2 component chạy song song cùng
    /// gọi service dùng chung context, sẽ crash "A second operation was started...".
    ///
    /// Đăng ký class này là Scoped (cùng vòng đời với DbContext) để mọi service trong cùng 1 circuit
    /// dùng chung đúng 1 instance semaphore, ép các thao tác DB chạy TUẦN TỰ thay vì song song.
    ///
    /// Cách dùng: await using (await gate.EnterAsync()) { ... thao tác _db ở đây ... }
    /// </summary>
    public sealed class DbConcurrencyGate
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<IAsyncDisposable> EnterAsync()
        {
            await _semaphore.WaitAsync();
            return new Releaser(_semaphore);
        }

        private sealed class Releaser : IAsyncDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;
            public ValueTask DisposeAsync()
            {
                _semaphore.Release();
                return ValueTask.CompletedTask;
            }
        }
    }
}