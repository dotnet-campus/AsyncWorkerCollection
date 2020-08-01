using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 异步任务队列，这是重量级的方案，将会开启一个线程来做
    /// </summary>
    public class AsyncTaskQueue : IDisposable
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
        /// <returns>isInvalid:异步操作是否有效(多任务时，如果设置了<see cref="AutoCancelPreviousTask"/>,只会保留最后一个任务有效)；result:异步操作结果</returns>
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
            lock (_queue)
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
            while (!_isDisposed)
            {
                if (_queue.Count == 0)
                {
                    //等待后续任务
                    await _autoResetEvent.WaitOneAsync();
                }

                while (TryGetNextTask(out var task))
                {
                    //如已从队列中删除
                    if (!task.Executable) continue;
                    //添加是否已释放的判断
                    if (!_isDisposed)
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
            if (disposing)
            {
                //_autoResetEvent.Dispose();
            }

            _queue.Clear();
            _autoResetEvent = null;
            _isDisposed = true;
        }

        #endregion

        #region 属性及字段

        /// <summary>
        /// 是否使用单线程完成任务.
        /// </summary>
        public bool UseSingleThread { get; set; } = true;

        /// <summary>
        /// 自动取消以前的任务。
        /// </summary>
        public bool AutoCancelPreviousTask { get; set; } = false;

        private bool _isDisposed;
        private readonly ConcurrentQueue<AwaitableTask> _queue = new ConcurrentQueue<AwaitableTask>();
        private AsyncAutoResetEvent _autoResetEvent;

        #endregion
    }
}