using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 异步任务队列，将任务加入到队列里面按照顺序执行
    /// </summary>
#if PublicAsInternal
    internal
#else
    public
#endif
    class AsyncTaskQueue : IDisposable
    {
        /// <summary>
        /// 异步任务队列
        /// </summary>
        public AsyncTaskQueue()
        {
            _autoResetEvent = new AsyncAutoResetEvent(false);
            InternalRunning();
        }

        #region 执行

        /// <summary>
        /// 执行异步操作
        /// </summary>
        /// <typeparam name="T">返回结果类型</typeparam>
        /// <param name="func">异步操作</param>
        /// <returns>IsInvalid:异步操作是否有效(多任务时，如果设置了<see cref="AutoCancelPreviousTask"/>，只会保留最后一个任务有效)；Result:异步操作结果</returns>
        public async Task<(bool IsInvalid, T Result)> ExecuteAsync<T>(Func<Task<T>> func)
        {
            var task = GetExecutableTask(func);
            var result = await await task;
            if (!task.IsValid)
            {
                result = default;
            }

            return (task.IsValid, result);
        }

        /// <summary>
        /// 执行异步操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedTypeParameter
        public async Task<bool> ExecuteAsync<T>(Func<Task> func)
        {
            var task = GetExecutableTask(func);
            await await task;
            return task.IsValid;
        }

        #endregion

        #region 添加任务

        /// <summary>
        /// 获取待执行任务
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private AwaitableTask GetExecutableTask(Action action)
        {
            var awaitableTask = new AwaitableTask(new Task(action));
            AddPendingTaskToQueue(awaitableTask);
            return awaitableTask;
        }

        /// <summary>
        /// 获取待执行任务
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        private AwaitableTask<TResult> GetExecutableTask<TResult>(Func<TResult> function)
        {
            var awaitableTask = new AwaitableTask<TResult>(new Task<TResult>(function));
            AddPendingTaskToQueue(awaitableTask);
            return awaitableTask;
        }

        /// <summary>
        /// 添加待执行任务到队列
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private void AddPendingTaskToQueue(AwaitableTask task)
        {
            //添加队列，加锁。
            lock (Locker)
            {
                _queue.Enqueue(task);
                //开始执行任务
                _autoResetEvent.Set();
            }
        }

        #endregion

        #region 内部运行

        private async void InternalRunning()
        {
            while (!_isDisposing)
            {
                Task<bool> waitOneTask = null;
                bool shouldWaitOneTask = _queue.Count == 0;

                lock (Locker)
                {
                    if (_isDisposed)
                    {
                        Debug.Assert(_isDisposing);
                        return;
                    }

                    // 在锁里获取异步锁，这样可以解决在释放的时候，调用异步锁已被释放
                    if (shouldWaitOneTask)
                    {
                        waitOneTask = _autoResetEvent.WaitOneAsync();
                    }
                }

                if (shouldWaitOneTask)
                {
                    //等待后续任务
                    await waitOneTask.ConfigureAwait(false);
                }

                while (TryGetNextTask(out var task))
                {
                    //如已从队列中删除
                    if (!task.Executable) continue;
                    //添加是否已释放的判断
                    if (!_isDisposing)
                    {
                        if (UseSingleThread)
                        {
                            task.RunSynchronously();
                        }
                        else
                        {
                            task.Start();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 上一次异步操作
        /// </summary>
        private AwaitableTask _lastDoingTask;

        private bool TryGetNextTask(out AwaitableTask task)
        {
            task = null;
            while (_queue.Count > 0)
            {
                //获取并从队列中移除任务
                if (_queue.TryDequeue(out task) && (!AutoCancelPreviousTask || _queue.Count == 0))
                {
                    //设置进行中的异步操作无效
                    _lastDoingTask?.MarkTaskInvalid();
                    _lastDoingTask = task;
                    return true;
                }

                Debug.Assert(task != null);
                //并发操作，设置任务不可执行
                task.SetNotExecutable();
            }

            return false;
        }

        #endregion

        #region dispose

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 析构任务队列
        /// </summary>
        ~AsyncTaskQueue()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            lock (Locker)
            {
                if (_isDisposed) return;
                _isDisposing = true;
                if (disposing)
                {
                }

                // 先调用 Clear 方法，然后调用  _autoResetEvent.Dispose 此时的任务如果还没执行的，就不会执行
                _queue.Clear();
                _autoResetEvent.Dispose();
                _isDisposed = true;
            }
        }

        #endregion

        #region 属性及字段

        /// <summary>
        /// 是否使用单线程完成任务
        /// </summary>
        public bool UseSingleThread { get; set; } = true;

        /// <summary>
        /// 自动取消以前的任务，此属性应该是在创建对象完成之后给定，不允许在任务执行过程中更改
        /// </summary>
        /// 设置和获取不需要加上锁，因为这是原子的，业务上也不会有开发者不断修改这个值。也就是说这个属性只有在对象创建就给定
        public bool AutoCancelPreviousTask
        {
            get => _autoCancelPreviousTask;
            set
            {
                if (_lastDoingTask != null)
                {
                    // 仅用于开发时告诉开发者，在任务开始之后调用是不对的
                    throw new InvalidOperationException($"此属性应该是在创建对象完成之后给定，不允许在任务执行过程中更改");
                }

                _autoCancelPreviousTask = value;
            }
        }

        private object Locker => _queue;
        private bool _isDisposed;
        private bool _isDisposing;
        private readonly ConcurrentQueue<AwaitableTask> _queue = new ConcurrentQueue<AwaitableTask>();
        private readonly AsyncAutoResetEvent _autoResetEvent;
        // ReSharper disable once RedundantDefaultMemberInitializer
        private bool _autoCancelPreviousTask = false;

        #endregion
    }
}
