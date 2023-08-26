using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 提供双缓存 线程安全列表
    /// </summary>
    /// <typeparam name="T">用于存放 <typeparamref name="TU"/> 的集合</typeparam>
    /// <typeparam name="TU"></typeparam>
    /// 写入的时候写入到一个列表，通过 SwitchBuffer 方法，可以切换当前缓存
#if PublicAsInternal
    internal
#else
    public
#endif
        class DoubleBuffer<T, TU> where T : class, ICollection<TU>
    {
        /// <summary>
        /// 创建双缓存
        /// </summary>
        /// <param name="aList"></param>
        /// <param name="bList"></param>
        public DoubleBuffer(T aList, T bList)
        {
            AList = aList;
            BList = bList;

            CurrentList = AList;
        }

        /// <summary>
        /// 加入元素到缓存
        /// </summary>
        /// <param name="t"></param>
        public void Add(TU t)
        {
            lock (_lock)
            {
                CurrentList.Add(t);
            }
        }

        /// <summary>
        /// 切换缓存
        /// </summary>
        /// <returns></returns>
        public T SwitchBuffer()
        {
            lock (_lock)
            {
                CurrentList = ReferenceEquals(CurrentList, AList) ? AList : BList;
                return ReferenceEquals(CurrentList, AList) ? BList : AList;
            }
        }

        /// <summary>
        /// 执行完所有任务
        /// </summary>
        /// <param name="action">当前缓存里面存在的任务，请不要保存传入的 List 参数</param>
        public void DoAll(Action<T> action)
        {
            while (true)
            {
                var buffer = SwitchBuffer();
                if (buffer.Count == 0) break;

                action(buffer);
                buffer.Clear();
            }
        }

        /// <summary>
        /// 执行完所有任务
        /// </summary>
        /// <param name="action">当前缓存里面存在的任务，请不要保存传入的 List 参数</param>
        /// <returns></returns>
        public async Task DoAllAsync(Func<T, Task> action)
        {
            while (true)
            {
                var buffer = SwitchBuffer();
                if (buffer.Count == 0) break;

                await action(buffer).ConfigureAwait(false);
                buffer.Clear();
            }
        }

        /// <summary>
        /// 获取当前是否为空，线程不安全，必须自行加锁
        /// </summary>
        /// <returns></returns>
        internal bool GetIsEmpty()
        {
            return AList.Count == 0 && BList.Count == 0;
        }

        /// <summary>
        /// 用于给其他类型的同步使用的对象
        /// </summary>
        internal object SyncObject => _lock;

        private readonly object _lock = new object();

        private T CurrentList { set; get; }

        private T AList { get; }
        private T BList { get; }
    }
}
