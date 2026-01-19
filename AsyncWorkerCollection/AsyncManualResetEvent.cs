using System.Threading;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 异步等待的manualresetevent
    /// WaitOneAsync方法会返回一个task，通过await方式等待
    /// </summary>
#if PublicAsInternal
    internal
#else
    public
#endif
    class AsyncManualResetEvent
    {
        /// <summary>
        /// 提供一个信号初始值，确定是否有信号
        /// </summary>
        /// <param name="initialState">true为有信号，所有等待可以直接通过</param>
        public AsyncManualResetEvent(bool initialState)
        {
            _source = new TaskCompletionSource<bool>();

            if (initialState)
            {
                _source.SetResult(true);
            }
        }

        /// <summary>
        /// 异步等待一个信号，需要await
        /// </summary>
        /// <returns></returns>
        public Task WaitOneAsync()
        {
            return _source.Task;
        }

        /// <summary>
        /// 设置一个信号量，所有等待获得信号
        /// </summary>
        public void Set()
        {
            _source.TrySetResult(true);
        }

        /// <summary>
        /// 设置一个信号量，所有wait等待
        /// </summary>
        public void Reset()
        {
            while (true)
            {
                var tcs = _source;
                if (!tcs.Task.IsCompleted)
                {
                    return;
                }

                var newTcs = new TaskCompletionSource<bool>();
                if (Interlocked.CompareExchange(ref _source, newTcs, tcs) == tcs)
                {
                    return;
                }
            }
        }

        private volatile TaskCompletionSource<bool> _source;
    }
}
