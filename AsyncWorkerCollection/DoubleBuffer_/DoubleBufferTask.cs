#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if !NETCOREAPP
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 双缓存任务
    /// </summary>
#if PublicAsInternal
    internal
#else
    public
#endif
    class DoubleBufferTask<T, TU> : IAsyncDisposable
        where T : class, ICollection<TU>
    {
        /// <summary>
        /// 创建双缓存任务，执行任务的方法放在 <paramref name="doTask"/> 方法
        /// </summary>
        /// <param name="doTask">
        /// 执行任务的方法
        /// <para></para>
        /// 传入的 List&lt;T&gt; 就是需要执行的任务，请不要将传入的 List&lt;T&gt; 保存到本地字段
        /// <para>
        /// 此委托需要自行完全处理异常，否则将会抛到后台线程
        /// </para>
        /// </param>
        /// <param name="aList"></param>
        /// <param name="bList"></param>
        public DoubleBufferTask(T aList, T bList, Func<T, Task> doTask)
        {
            _doTask = doTask;
            DoubleBuffer = new DoubleBuffer<T, TU>(aList, bList);
        }

        /// <summary>
        /// 加入任务
        /// </summary>
        /// <param name="t"></param>
        public void AddTask(TU t)
        {
            var isSetFinish = _isSetFinish;
            if (isSetFinish == 1)
            {
                // 被设置完成了，业务上就不应该再次给任何的数据内容
                throw new InvalidOperationException($"The DoubleBufferTask has been set finish.");
            }

            DoubleBuffer.Add(t);
            DoInner();
        }

        private async void DoInner()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            if (_isDoing) return;

            lock (Locker)
            {
                if (_isDoing) return;
                _isDoing = true;
            }

            while (true)
            {
                await DoubleBuffer.DoAllAsync(_doTask).ConfigureAwait(false);

                lock (Locker)
                {
                    if (DoubleBuffer.GetIsEmpty())
                    {
                        _isDoing = false;
                        Finished?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public void Finish()
        {
            var isSetFinish = Interlocked.CompareExchange(ref _isSetFinish, 1, 0);
            if (isSetFinish == 1)
            {
                // 多次设置完成任务
                // 重复多次调用 Finish 方法，第二次调用将无效
                return;
            }

            lock (Locker)
            {
                if (!_isDoing)
                {
                    FinishTask.SetResult(true);
                    return;
                }

                Finished += OnFinished;
            }
        }

        private void OnFinished(object sender, EventArgs args)
        {
            Finished -= OnFinished;
            FinishTask.SetResult(true);
        }

        /// <summary>
        /// 等待完成任务，只有在调用 <see cref="Finish"/> 之后，所有任务执行完成才能完成
        /// </summary>
        /// <returns></returns>
        public Task WaitAllTaskFinish()
        {
            return FinishTask.Task;
        }

        private TaskCompletionSource<bool> FinishTask { get; } = new TaskCompletionSource<bool>();

        /// <summary>
        /// 是否调用了 <see cref="Finish"/> 方法，因为此方法不适合多次重复调用
        /// </summary>
        /// 选用 int 的考虑是为了做原子无锁设计，提升性能
        private int _isSetFinish;

        private volatile bool _isDoing;

        private event EventHandler? Finished;

        private readonly Func<T, Task> _doTask;

        private DoubleBuffer<T, TU> DoubleBuffer { get; }
        private object Locker => DoubleBuffer.SyncObject;

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            Finish();
            await WaitAllTaskFinish().ConfigureAwait(false);
        }
    }
}
