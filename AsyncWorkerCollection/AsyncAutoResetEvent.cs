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

        private static readonly Task CompletedSourceTask = Task.FromResult(true);

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
                    _isSignaled = false;
                    return CompletedSourceTask;
                }

                var source = new TaskCompletionSource<bool>();
                _waitQueue.Enqueue(source);
                return source.Task;
            }
        }

        /// <summary>
        /// 设置一个信号量，让一个waitone获得信号
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

        private bool _isSignaled;
    }
}