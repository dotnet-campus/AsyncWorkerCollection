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
           var currentCount = Interlocked.Increment(ref _doingTaskCount);

            DoubleBuffer.Add(t);

            if (currentCount == 1)
            {
                DoInner();
            }
        }

        /// <summary>
        /// 当前正在排队执行的任务数量
        /// </summary>
        /// @太子：用 int 就足够了，没有那么多内存可以用到 long 那么多
        private int _doingTaskCount;

        private async void DoInner()
        {
            await DoubleBuffer.DoAllAsync(t =>
            {
                Interlocked.Add(ref _doingTaskCount, -t.Count);
                return _doTask(t);
            }).ConfigureAwait(false);

            if (_isDoing) return;

            lock (DoubleBuffer) 
            {
                if (_isDoing) return;
                _isDoing = true;
            }

            while (true)
            {
                

                lock (DoubleBuffer)
                {
                    if (DoubleBuffer.GetIsEmpty())
                    {
                        // A
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
            lock (DoubleBuffer)
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

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            Finish();
            await WaitAllTaskFinish().ConfigureAwait(false);
        }
    }
}
