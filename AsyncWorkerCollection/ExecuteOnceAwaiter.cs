using System;
using System.Threading;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    public class ExecuteOnceAwaiter<TResult>
    {
        public ExecuteOnceAwaiter(Func<Task<TResult>> asyncAction)
        {
            _asyncAction = asyncAction;
        }

        public Task<TResult> ExecuteAsync()
        {
            if (_executionResult != null)
            {
                return _executionResult;
            }

            Lock(() => _executionResult = _asyncAction());

            return _executionResult;
        }

        public void ResetWhileCompleted()
        {
            if (IsCompleted)
            {
                Lock(() => _executionResult = null);
            }
        }

        private readonly Func<Task<TResult>> _asyncAction;
        private Task<TResult> _executionResult;
        private SpinLock _spinLock = new SpinLock(true);

        private bool IsRunning => _executionResult?.IsCompleted is false;

        private bool IsCompleted => _executionResult?.IsCompleted is true;

        private void Lock(Action action)
        {
            var lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                action();
            }
            finally
            {
                // 参数错误或锁递归时才会发生异常，所以此处几乎能肯定 lockTaken 为 true。
                if (lockTaken) _spinLock.Exit();
            }
        }
    }
}