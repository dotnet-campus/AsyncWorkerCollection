using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 异步等待的autoresetevent
    /// WaitOneAsync方法会返回一个task，通过await方式等待
    /// </summary>
    public class AsyncAutoResetEvent
    {
        /// <summary>
        /// 提供一个信号初始值，确定是否有信号
        /// </summary>
        /// <param name="initialState">true为有信号，第一个等待可以直接通过</param>
        public AsyncAutoResetEvent(bool initialState)
        {
            _isSignaled = initialState;
        }

        private static readonly Task CompletedSourceTask
#if NETFRAMEWORK
            = Task.FromResult(true);
#else
            = Task.CompletedTask;
#endif


        /// <summary>
        /// 异步等待一个信号，需要await
        /// </summary>
        /// <returns></returns>
        public Task WaitOneAsync()
        {
            lock (_locker)
            {
                if (_isSignaled)
                {
                    // 按照 AutoResetEvent 的设计，在没有任何等待进入时，如果有设置 Set 方法，那么下一次第一个进入的等待将会通过
                    // 也就是在没有任何等待时，无论调用多少次 Set 方法，在调用之后只有一个等待通过
                    _isSignaled = false;
                    return CompletedSourceTask;
                }

                var source = new TaskCompletionSource<bool>();
                _waitQueue.Enqueue(source);
                return source.Task;
            }
        }

        /// <summary>
        /// 设置一个信号量，让一个waitone获得信号，每次调用 <see cref="Set"/> 方法最多只有一个等待通过
        /// </summary>
        public void Set()
        {
            TaskCompletionSource<bool> releaseSource = null;
            lock (_locker)
            {
                if (_waitQueue.Any())
                {
                    releaseSource = _waitQueue.Dequeue();
                }

                if (releaseSource is null)
                {
                    if (!_isSignaled)
                    {
                        _isSignaled = true;
                    }
                }
            }

            releaseSource?.SetResult(true);
        }

        private readonly object _locker = new object();

        private readonly Queue<TaskCompletionSource<bool>> _waitQueue =
            new Queue<TaskCompletionSource<bool>>();

        /// <summary>
        /// 用于在没有任何等待时让下一次等待通过
        /// </summary>
        private bool _isSignaled;
    }
}