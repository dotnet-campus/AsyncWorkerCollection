using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
