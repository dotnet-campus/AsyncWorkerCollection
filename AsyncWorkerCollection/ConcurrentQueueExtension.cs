using System.Collections.Concurrent;

namespace dotnetCampus.Threading
{
    static class ConcurrentQueueExtension
    {
        // 在 .NET Framework 4.5 没有清理方法
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out _))
            {
            }
        }
    }
}
