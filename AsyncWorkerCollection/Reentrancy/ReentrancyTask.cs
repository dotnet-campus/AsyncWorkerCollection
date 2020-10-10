using System;
using System.Threading.Tasks;

namespace dotnetCampus.Threading.Reentrancy
{
    /// <summary>
    /// 表示一个可重入任务。使用不同的可重入任务子类，你可以使用不同的重入策略处理并发任务的重入问题。
    /// </summary>
    /// <typeparam name="TParameter">
    /// 重入任务中单次执行时所使用的参数。
    /// 注意，对于部分类型的重入任务，参数可能会被选择性忽略；具体取决于不同的重入策略是否会导致任务是否全部被执行。
    /// </typeparam>
    /// <typeparam name="TReturn">
    /// 重入任务中单次执行时所得到的返回值。
    /// 注意，对于部分类型的重入任务，返回值可能会是此类型的默认值；具体取决于不同的重入策略是否会导致任务是否全部被执行。
    /// </typeparam>
#if PublicAsInternal
    internal
#else
    public
#endif
    abstract class ReentrancyTask<TParameter, TReturn>
    {
        /// <summary>
        /// 在派生类中执行重入任务的时候，从此处获取需要执行的可重入异步任务。
        /// </summary>
        protected Func<TParameter, Task<TReturn>> WorkingTask { get; }

        /// <summary>
        /// 初始化可重入任务的公共基类。
        /// </summary>
        /// <param name="task">可重入任务本身。</param>
        protected ReentrancyTask(Func<TParameter, Task<TReturn>> task)
        {
            WorkingTask = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// 执行重入任务，并获取此次重入任务的返回值。
        /// 如果此次任务不被执行，那么将返回类型的默认值。
        /// </summary>
        /// <param name="arg">此次重入任务使用的参数。</param>
        /// <returns>重入任务当次执行的返回值。</returns>
        public abstract Task<TReturn> InvokeAsync(TParameter arg);

        /// <summary>
        /// 执行实际的异步任务，也就是用户部分的代码。
        /// </summary>
        /// <param name="arg">此次重入任务使用的参数。</param>
        /// <returns>此次执行的返回值。</returns>
        protected Task<TReturn> RunCore(TParameter arg) => WorkingTask(arg);
    }
}
