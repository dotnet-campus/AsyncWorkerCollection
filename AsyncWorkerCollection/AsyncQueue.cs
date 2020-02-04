using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    public class AsyncQueue<T>
    {
        public AsyncQueue()
        {
            _sem = new SemaphoreSlim(0);
            _que = new ConcurrentQueue<T>();
        }

        public void Enqueue(T item)
        {
            _que.Enqueue(item);
            _sem.Release();
        }

        public void EnqueueRange(IEnumerable<T> source)
        {
            var n = 0;
            foreach (var item in source)
            {
                _que.Enqueue(item);
                n++;
            }

            _sem.Release(n);
        }

        public async Task<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            for (;;)
            {
                await _sem.WaitAsync(cancellationToken);

                if (_que.TryDequeue(out var item))
                {
                    return item;
                }
            }
        }

        private readonly ConcurrentQueue<T> _que;
        private readonly SemaphoreSlim _sem;
    }
}