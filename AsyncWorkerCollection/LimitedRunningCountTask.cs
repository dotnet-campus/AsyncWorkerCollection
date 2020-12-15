#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if !NETCOREAPP
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 限制执行数量的任务，执行的任务超过设置的数量将可以等待直到正在执行任务数小于设置的数量
    /// </summary>
#if PublicAsInternal
    internal
#else
    public
#endif
        class LimitedRunningCountTask
    {
        /// <summary>
        /// 创建限制执行数量的任务
        /// </summary>
        /// <param name="maxRunningCount">允许最大的执行数量的任务</param>
        public LimitedRunningCountTask(uint maxRunningCount)
        {
            MaxRunningCount = maxRunningCount;
        }

        /// <summary>
        /// 执行的任务数
        /// </summary>
        public int RunningCount
        {
            set
            {
                lock (Locker)
                {
                    _runningCount = value;
                }
            }
            get
            {
                lock (Locker)
                {
                    return _runningCount;
                }
            }
        }

        /// <summary>
        /// 允许最大的执行数量的任务
        /// </summary>
        public uint MaxRunningCount { get; }

        /// <summary>
        /// 加入执行任务
        /// </summary>
        /// <param name="task"></param>
        public void Add(Task task)
        {
            RunningCount++;
            lock (Locker)
            {
                Buffer.Add(task);

                RunningBreakTask?.TrySetResult(true);
            }

            RunningInner();
        }

        /// <summary>
        /// 加入等待任务，在空闲之后等待才会返回
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public async ValueTask AddAsync(Task task)
        {
            // ReSharper disable once MethodHasAsyncOverload
            Add(task);
            await WaitForFree().ConfigureAwait(false);
        }

        /// <summary>
        /// 等待空闲
        /// </summary>
        /// <returns></returns>
        public async ValueTask WaitForFree()
        {
            if (WaitForFreeTask == null)
            {
                return;
            }

            await WaitForFreeTask.Task.ConfigureAwait(false);
        }

        private TaskCompletionSource<bool>? RunningBreakTask
        {
            set
            {
                lock (Locker)
                {
                    _runningBreakTask = value;
                }
            }
            get
            {
                lock (Locker)
                {
                    return _runningBreakTask;
                }
            }
        }

        private TaskCompletionSource<bool>? WaitForFreeTask
        {
            set
            {
                lock (Locker)
                {
                    _waitForFreeTask = value;
                }
            }
            get
            {
                lock (Locker)
                {
                    return _waitForFreeTask;
                }
            }
        }

        private List<Task> Buffer { get; } = new List<Task>();

        private object Locker => Buffer;

        private bool _isRunning;

        private int _runningCount;

        private TaskCompletionSource<bool>? _runningBreakTask;

        private TaskCompletionSource<bool>? _waitForFreeTask;

        private async void RunningInner()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            if (_isRunning)
            {
                return;
            }

            lock (Locker)
            {
                if (_isRunning)
                {
                    return;
                }

                _isRunning = true;
            }

            List<Task> runningTaskList;
            lock (Locker)
            {
                runningTaskList = Buffer.ToList();
                Buffer.Clear();
                RunningBreakTask = new TaskCompletionSource<bool>();
                runningTaskList.Add(RunningBreakTask.Task);

                SetWaitForFreeTask();
            }

            while (runningTaskList.Count > 0)
            {
                // 加入等待
                await Task.WhenAny(runningTaskList).ConfigureAwait(false);

                // 干掉不需要的任务
                runningTaskList.RemoveAll(task => task.IsCompleted);

                lock (Locker)
                {
                    runningTaskList.AddRange(Buffer);
                    Buffer.Clear();

                    RunningCount = runningTaskList.Count;

                    if (!RunningBreakTask.Task.IsCompleted)
                    {
                        runningTaskList.Add(RunningBreakTask.Task);
                    }
                    else
                    {
                        RunningBreakTask = new TaskCompletionSource<bool>();
                        runningTaskList.Add(RunningBreakTask.Task);
                    }

                    if (runningTaskList.Count < MaxRunningCount)
                    {
                        WaitForFreeTask?.TrySetResult(true);
                    }
                    else
                    {
                        SetWaitForFreeTask();
                    }
                }
            }

            lock (Locker)
            {
                _isRunning = false;
            }

            void SetWaitForFreeTask()
            {
                if (runningTaskList.Count > MaxRunningCount)
                {
                    if (WaitForFreeTask?.Task.IsCompleted is false)
                    {
                    }
                    else
                    {
                        WaitForFreeTask = new TaskCompletionSource<bool>();
                    }
                }
            }
        }
    }
}
