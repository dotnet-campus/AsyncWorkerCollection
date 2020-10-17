using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 可等待的任务
    /// </summary>
#if PublicAsInternal
    internal
#else
    public
#endif
    class AwaitableTask
    {
        /// <summary>
        /// 获取任务是否为不可执行状态
        /// </summary>
        public bool Executable { get; private set; } = true;

        /// <summary>
        /// 获取任务是否有效
        /// 注：对无效任务，可以不做处理。减少并发操作导致的干扰
        /// </summary>
        public bool IsValid { get; private set; } = true;

        /// <summary>
        /// 设置任务不可执行
        /// </summary>
        public void SetNotExecutable()
        {
            Executable = false;
        }

        /// <summary>
        /// 标记任务无效
        /// </summary>
        public void MarkTaskInvalid()
        {
            IsValid = false;
        }

        #region Task

        private readonly Task _task;

        /// <summary>
        /// 初始化可等待的任务。
        /// </summary>
        /// <param name="task"></param>
        public AwaitableTask(Task task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// 获取任务是否已完成
        /// </summary>
        public bool IsCompleted => _task.IsCompleted;

        /// <summary>
        /// 任务的Id
        /// </summary>
        public int TaskId => _task.Id;

        /// <summary>
        /// 开始任务
        /// </summary>
        public void Start()
        {
            _task.Start();
        }

        /// <summary>
        /// 同步执行开始任务
        /// </summary>
        public void RunSynchronously()
        {
            _task.RunSynchronously();
        }

        #endregion

        #region TaskAwaiter

        /// <summary>
        /// 获取任务等待器
        /// </summary>
        /// <returns></returns>
        public TaskAwaiter GetAwaiter()
        {
            return new TaskAwaiter(this);
        }

        /// <summary>Provides an object that waits for the completion of an asynchronous task. </summary>
        //[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true, Synchronization = true)]
        public struct TaskAwaiter : INotifyCompletion
        {
            private readonly AwaitableTask _awaitableTask;

            /// <summary>
            /// 任务等待器
            /// </summary>
            /// <param name="awaitableTask"></param>
            public TaskAwaiter(AwaitableTask awaitableTask)
            {
                _awaitableTask = awaitableTask;
            }

            /// <summary>
            /// 任务是否完成.
            /// </summary>
            public bool IsCompleted => _awaitableTask._task.IsCompleted;

            /// <inheritdoc />
            public void OnCompleted(Action continuation)
            {
                var This = this;
                _awaitableTask._task.ContinueWith(t =>
                {
                    if (This._awaitableTask.Executable) continuation?.Invoke();
                });
            }

            /// <summary>
            /// 获取任务结果
            /// </summary>
            public void GetResult()
            {
                _awaitableTask._task.Wait();
            }
        }

        #endregion
    }
}
