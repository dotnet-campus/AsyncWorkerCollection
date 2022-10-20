using System;
using System.Threading;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 只执行一次的等待
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
#if PublicAsInternal
    internal
#else
    public
#endif
        class ExecuteOnceAwaiter<TResult>
    {
        /// <summary>
        /// 创建只执行一次的等待，调用 <see cref="ExecuteAsync"/> 时，无论调用多少次，只会执行 <paramref name="asyncAction"/> 一次
        /// <para></para>
        /// 因为此类使用了锁，因此需要调用方处理 <paramref name="asyncAction"/> 自身线程安全问题
        /// </summary>
        /// <param name="asyncAction">执行的具体逻辑，需要调用方处理自身线程安全问题</param>
        public ExecuteOnceAwaiter(Func<Task<TResult>> asyncAction)
        {
            _asyncAction = asyncAction;
        }

        /// <summary>
        /// 执行传入的具体逻辑，无论多少线程多少次调用，传入的具体逻辑只会执行一次
        /// </summary>
        /// <returns></returns>
        public Task<TResult> ExecuteAsync()
        {
            lock (_locker)
            {
                if (_executionResult is not null)
                {
                    return _executionResult;
                }

                _executionResult = _asyncAction();
                return _executionResult;
            }
        }

        /// <summary>
        /// 在传入的具体逻辑执行完成之后，设置允许重新执行。如果此具体逻辑还在执行中，那么此方法调用无效
        /// </summary>
        public void ResetWhileCompleted()
        {
            lock (_locker)
            {
                if (_executionResult?.IsCompleted is true)
                {
                    _executionResult = default;
                }
            }
        }

        private readonly object _locker = new();

        private readonly Func<Task<TResult>> _asyncAction;

        private Task<TResult> _executionResult;
    }
}
