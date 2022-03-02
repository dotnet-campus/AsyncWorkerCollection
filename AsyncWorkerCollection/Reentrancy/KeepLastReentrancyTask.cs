using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace dotnetCampus.Threading.Reentrancy
{
    /// <summary>
    /// 执行当前队列中的最后一个任务，并对所有当前队列任务赋值该任务结果。
    /// </summary>
    /// <typeparam name="TParameter">
    /// 重入任务中单次执行时所使用的参数。
    /// 此重入策略不会忽略任何参数。
    /// </typeparam>
    /// <typeparam name="TReturn">
    /// 重入任务中单次执行时所得到的返回值。
    /// 此重入策略不会忽略任何返回值。
    /// </typeparam>
#if PublicAsInternal
    internal
#else
    public
#endif
    sealed class KeepLastReentrancyTask<TParameter, TReturn> : ReentrancyTask<TParameter, TReturn>
    {
        /// <summary>
        /// 用于原子操作判断当前是否正在执行队列中的可重入任务。
        /// 1 表示当前正在执行可重入任务；0 表示不确定。
        /// 不可使用 bool 类型，因为 bool 类型无法执行可靠的原子操作。
        /// </summary>
        private volatile int _isRunning;

        /// <summary>
        /// 由于原子操作仅提供高性能的并发处理而不保证准确性，因此需要一个锁来同步 <see cref="_isRunning"/> 中值为 0 时所指的不确定情况。
        /// 不能使用一个锁来同步所有情况是因为在锁中使用 async/await 是不安全的，因此避免在锁中执行异步任务；我们使用原子操作来判断异步任务的执行条件。
        /// </summary>
        private readonly object _locker = new object();

        /// <summary>
        /// 使用一个并发队列来表示目前已加入到队列中的全部可重入任务。
        /// 因为我们的 <see cref="_locker"/> 不能锁全部队列操作（原因见 <see cref="_locker"/>），因此需要使用并发队列。
        /// </summary>
        private readonly ConcurrentQueue<TaskWrapper> _queue = new ConcurrentQueue<TaskWrapper>();

        /// <summary>
        /// 使用一个队列表示当前执行任务开始时所有需要进行赋值结果的任务。
        /// </summary>
        private readonly Queue<TaskWrapper> _skipQueue = new Queue<TaskWrapper>();

        private readonly bool _configureAwait;

        /// <summary>
        /// 创建以KeepLast策略执行的可重入任务。
        /// </summary>
        /// <param name="task">可重入任务本身。</param>
        public KeepLastReentrancyTask(Func<TParameter, Task<TReturn>> task) : base(task) { }

        /// <summary>
        /// 创建以KeepLast策略执行的可重入任务。
        /// </summary>
        /// <param name="task">可重入任务本身。</param>
        /// <param name="configureAwait"></param>
        public KeepLastReentrancyTask(Func<TParameter, Task<TReturn>> task, bool configureAwait) : this(task)
        {
            _configureAwait = configureAwait;
        }

        /// <summary>
        /// 以KeepLast策略执行重入任务，并获取此次重入任务的返回值。
        /// 此重入策略会确保执行当前队列中的最后一个任务，并对所有当前队列任务赋值该任务结果。
        /// </summary>
        /// <param name="arg">此次重入任务使用的参数。</param>
        /// <returns>重入任务本次执行的返回值。</returns>
        public override Task<TReturn> InvokeAsync(TParameter arg)
        {
            var wrapper = new TaskWrapper(() => RunCore(arg), _configureAwait);
            _queue.Enqueue(wrapper);
            Run();
            return wrapper.AsTask();
        }

        /// <summary>
        /// 以KeepLast策略执行重入任务。此方法确保线程安全。
        /// </summary>
        private async void Run()
        {
            var isRunning = Interlocked.CompareExchange(ref _isRunning, 1, 0);
            if (isRunning is 1)
            {
                lock (_locker)
                {
                    if (_isRunning is 1)
                    {
                        // 当前已经在执行队列，因此无需继续执行。
                        return;
                    }
                }
            }

            //下面这段是在临界区执行的，不存在多线程问题
            var hasTask = true;
            while (hasTask)
            {
                TaskWrapper runTask = null;
                // 当前还没有任何队列开始执行，因此需要开始执行队列。
                while (_queue.TryDequeue(out var wrapper))
                {
                    //所有任务项转入执行队列
                    if (runTask != null)
                    {
                        _skipQueue.Enqueue(runTask);
                    }

                    runTask = wrapper;
                }

                if (runTask != null)
                {
                    // 内部已包含异常处理，因此外面可以无需捕获或者清理。
                    await runTask.RunAsync().ConfigureAwait(_configureAwait);
                    //完成后对等待队列中的项赋值
                    if (runTask.Exception != null)
                    {
                        SetException(runTask.Exception);
                    }
                    else
                    {
                        SetResult(runTask.Result);
                    }
                }

                lock (_locker)
                {
                    hasTask = _queue.TryPeek(out _);
                    if (!hasTask)
                    {
                        //退出临界区
                        _isRunning = 0;
                    }
                }
            }
        }

        private void SetException(Exception exception)
        {
            while (_skipQueue.Count > 0)
            {
                var taskWrapper = _skipQueue.Dequeue();
                taskWrapper.SetException(exception);
            }
        }

        private void SetResult(TReturn result)
        {
            while (_skipQueue.Count > 0)
            {
                var taskWrapper = _skipQueue.Dequeue();
                taskWrapper.SetResult(result);
            }
        }

        /// <summary>
        /// 包装一个异步任务，以便在可以执行此异步任务的情况下可以在其他方法中监视其完成情况。
        /// </summary>
        private class TaskWrapper
        {
            /// <summary>
            /// 创建一个任务包装。
            /// </summary>
            internal TaskWrapper(Func<Task<TReturn>> workingTask, bool configureAwait)
            {
                _taskSource = new TaskCompletionSource<TReturn>();
                _task = workingTask;
                _configureAwait = configureAwait;
            }

            private readonly TaskCompletionSource<TReturn> _taskSource;
            private readonly Func<Task<TReturn>> _task;
            private readonly bool _configureAwait;

            public TReturn Result { get; set; }
            public Exception Exception { get; set; }

            /// <summary>
            /// 执行此异步任务。
            /// </summary>
            internal async Task RunAsync()
            {
                try
                {
                    var task = _task();
                    if (task is null)
                    {
                        throw new InvalidOperationException("在指定 KeepLastReentrancyTask 的任务时，方法内不允许返回 null。请至少返回 Task.FromResult<object>(null)。");
                    }
                    var result = await task.ConfigureAwait(_configureAwait);
                    _taskSource.SetResult(result);
                    Result = result;
                }
#pragma warning disable CA1031 // 异常已经被通知到异步代码中，因此此处无需处理异常。
                catch (Exception ex)
                {
                    _taskSource.SetException(ex);
                    Exception = ex;
                }
#pragma warning restore CA1031 // 异常已经被通知到异步代码中，因此此处无需处理异常。
            }

            public void SetResult(TReturn result)
            {
                if (_taskSource.Task.IsCompleted || _taskSource.Task.IsFaulted)
                {
                    return;
                }

                _taskSource.SetResult(result);
            }

            public void SetException(Exception exception)
            {
                if (_taskSource.Task.IsCompleted || _taskSource.Task.IsFaulted)
                {
                    return;
                }

                _taskSource.SetException(exception);
            }

            /// <summary>
            /// 将此异步包装器作为 <see cref="Task"/> 使用，以便获得 async/await 特性。
            /// </summary>
            internal Task<TReturn> AsTask() => _taskSource.Task;
        }
    }
}
