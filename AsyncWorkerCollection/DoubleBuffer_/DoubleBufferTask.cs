#nullable enable
using System;
using System.Collections.Generic;
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
            lock (Locker)
            {
                if (!_isDoing)
                {
                    FinishTask.SetResult(true);
                    return;
                }

                Finished += (sender, args) => FinishTask.SetResult(true);
            }
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
