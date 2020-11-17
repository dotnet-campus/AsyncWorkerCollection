#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if !NETCOREAPP
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 提供一个异步的队列。可以使用 await 关键字异步等待出队，当有元素入队的时候，等待就会完成。
    /// </summary>
    /// <typeparam name="T">存入异步队列中的元素类型。</typeparam>
#if PublicAsInternal
    internal
#else
    public
#endif
    class AsyncQueue<T> : IDisposable, IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ConcurrentQueue<T> _queue;

        /// <summary>
        /// 创建一个 <see cref="AsyncQueue{T}"/> 的新实例。
        /// </summary>
        public AsyncQueue()
        {
            _semaphoreSlim = new SemaphoreSlim(0);
            _queue = new ConcurrentQueue<T>();
        }

        /// <summary>
        /// 获取此刻队列中剩余元素的个数。
        /// 请注意：因为线程安全问题，此值获取后值即过时，所以获取此值的代码需要自行处理线程安全。
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 入队。
        /// </summary>
        /// <param name="item">要入队的元素。</param>
        public void Enqueue(T item)
        {
            ThrowIfDisposing();
            _queue.Enqueue(item);
            _semaphoreSlim.Release();
        }

        /// <summary>
        /// 将一组元素全部入队。
        /// </summary>
        /// <param name="source">要入队的元素序列。</param>
        public void EnqueueRange(IEnumerable<T> source)
        {
            ThrowIfDisposing();
            var n = 0;
            foreach (var item in source)
            {
                _queue.Enqueue(item);
                n++;
            }

            _semaphoreSlim.Release(n);
        }

        /// <summary>
        /// 异步等待出队。当队列中有新的元素时，异步等待就会返回。
        /// </summary>
        /// <param name="cancellationToken">
        /// 你可以通过此 <see cref="CancellationToken"/> 来取消等待出队。
        /// 由于此方法有返回值，后续方法可能依赖于此返回值，所以如果取消将抛出 <see cref="TaskCanceledException"/>。
        /// </param>
        /// <returns>可以异步等待的队列返回的元素。</returns>
        public async Task<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _dequeueAsyncEnterCount);
            try
            {
                while (!_isDisposed)
                {
                    await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                    if (_queue.TryDequeue(out var item))
                    {
                        return item;
                    }
                    else
                    {
                        // 当前没有任务
                        lock (_queue)
                        {
                            // 事件不是线程安全，因为存在事件的加等
                            CurrentFinished?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }

                return default!;
            }
            finally
            {
                Interlocked.Decrement(ref _dequeueAsyncEnterCount);
            }
        }

        /// <summary>
        /// 当前进入 <see cref="DequeueAsync"/> 还没被释放的次数
        /// </summary>
        private int _dequeueAsyncEnterCount;

        /// <summary>
        /// 等待当前的所有任务执行完成
        /// </summary>
        /// <returns></returns>
        public async ValueTask WaitForCurrentFinished()
        {
            if (_queue.Count == 0)
            {
                return;
            }

            using var currentFinishedTask = new CurrentFinishedTask(this);

            // 有线程执行事件触发，刚好此时在创建 CurrentFinishedTask 对象
            // 此时需要重新判断是否存在任务
            if (_queue.Count == 0)
            {
                return;
            }

            await currentFinishedTask.WaitForCurrentFinished();
        }

        /// <summary>
        /// 主要用来释放锁，让 DequeueAsync 方法返回，解决因为锁让此对象内存不释放
        /// <para></para>
        /// 这个方法不是线程安全
        /// </summary>
        public void Dispose()
        {
            ThrowIfDisposing();
            _isDisposing = true;

            // 当释放的时候，将通过 _queue 的 Clear 清空内容，而通过 _semaphoreSlim 的释放让 DequeueAsync 释放锁
            // 此时将会在 DequeueAsync 进入 TryDequeue 方法，也许此时依然有开发者在 _queue.Clear() 之后插入元素，但是没关系，我只是需要保证调用 Dispose 之后会让 DequeueAsync 方法返回而已
            _isDisposed = true;
            _queue.Clear();
            // 释放 DequeueAsync 方法，释放次数为 DequeueAsync 在调用的次数
            _semaphoreSlim.Release(_dequeueAsyncEnterCount);
            _semaphoreSlim.Dispose();
        }

        /// <summary>
        /// 等待任务执行完成之后返回，此方法不是线程安全
        /// <para></para>
        /// 如果在调用此方法同时添加任务，那么添加的任务存在线程安全
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            ThrowIfDisposing();
            _isDisposing = true;
            await WaitForCurrentFinished();

            // 在设置 _isDisposing 完成，刚好有 Enqueue 的代码
            if (_queue.Count != 0)
            {
                // 再次等待
                await WaitForCurrentFinished();
            }

            // 其实此时依然可以存在有线程在 Enqueue 执行，但是此时就忽略了

            // 设置变量，此时循环将会跳出
            _isDisposed = true;
            _semaphoreSlim.Release(int.MaxValue);
            _semaphoreSlim.Dispose();
        }

        // 这里忽略线程安全
        private void ThrowIfDisposing()
        {
            if (_isDisposing)
            {
                throw new ObjectDisposedException(nameof(AsyncQueue<T>));
            }
        }

        private event EventHandler? CurrentFinished;

        private bool _isDisposing;
        private bool _isDisposed;

        class CurrentFinishedTask : IDisposable
        {
            public CurrentFinishedTask(AsyncQueue<T> asyncQueue)
            {
                _asyncQueue = asyncQueue;

                lock (_asyncQueue)
                {
                    _asyncQueue.CurrentFinished += CurrentFinished;
                }
            }

            private void CurrentFinished(object sender, EventArgs e)
            {
                _currentFinishedTaskCompletionSource.TrySetResult(true);
            }

            public async ValueTask WaitForCurrentFinished()
            {
                await _currentFinishedTaskCompletionSource.Task;
            }

            private readonly TaskCompletionSource<bool> _currentFinishedTaskCompletionSource =
                new TaskCompletionSource<bool>();

            private readonly AsyncQueue<T> _asyncQueue;

            public void Dispose()
            {
                lock (_asyncQueue)
                {
                    _currentFinishedTaskCompletionSource.TrySetResult(true);
                    _asyncQueue.CurrentFinished -= CurrentFinished;
                }
            }
        }
    }
}
