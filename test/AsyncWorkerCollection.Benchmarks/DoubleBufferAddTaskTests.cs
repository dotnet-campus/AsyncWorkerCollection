using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using dotnetCampus.Threading;

namespace AsyncWorkerCollection.Benchmarks
{
    /// <summary>
    /// 加入任务时的性能测试
    /// </summary>
    public class DoubleBufferAddTaskTests
    {
        [Benchmark(Baseline = true)]
        public void AddTaskToConcurrentBag()
        {
            var foo = new Foo();
            var concurrentBag = new ConcurrentBag<Foo>();

            for (int i = 0; i < MaxCount; i++)
            {
                concurrentBag.Add(foo);
            }
        }

        /// <summary>
        /// 多线程加入数据到 ConcurrentBag 的方法
        /// </summary>
        [Benchmark()]
        public async Task AddTaskToConcurrentBagWithMultiThread()
        {
            var foo = new Foo();
            var concurrentBag = new ConcurrentBag<Foo>();

            var threadCount = 10;

            var taskList = new Task[threadCount];

            for (int j = 0; j < threadCount; j++)
            {
                var task = Task.Run(() =>
                {
                    for (int i = 0; i < MaxCount / threadCount; i++)
                    {
                        concurrentBag.Add(foo);
                    }
                });
                taskList[j] = task;
            }

            await Task.WhenAll(taskList);
        }

        [Benchmark()]
        public void AddTaskToDoubleBufferWithLock()
        {
            var foo = new Foo();
            var doubleBuffer = new DoubleBuffer<List<Foo>, Foo>(new List<Foo>(MaxCount), new List<Foo>(MaxCount));

            for (int i = 0; i < MaxCount; i++)
            {
                doubleBuffer.Add(foo);
            }
        }

        [Benchmark()]
        public async Task AddTaskToDoubleBufferWithLockMultiThread()
        {
            var foo = new Foo();
            var doubleBuffer = new DoubleBuffer<List<Foo>, Foo>(new List<Foo>(MaxCount), new List<Foo>(MaxCount));

            var threadCount = 10;

            var taskList = new Task[threadCount];

            for (int j = 0; j < threadCount; j++)
            {
                var task = Task.Run(() =>
                {
                    for (int i = 0; i < MaxCount / threadCount; i++)
                    {
                        doubleBuffer.Add(foo);
                    }
                });
                taskList[j] = task;
            }

            await Task.WhenAll(taskList);
        }

        /// <summary>
        /// 没有给定数组的长度
        /// </summary>
        /// <returns></returns>
        [Benchmark()]
        public async Task AddTaskToDoubleBufferWithoutCapacityMultiThread()
        {
            var foo = new Foo();
            var doubleBuffer = new DoubleBuffer<Foo>();

            var threadCount = 10;

            var taskList = new Task[threadCount];

            for (int j = 0; j < threadCount; j++)
            {
                var task = Task.Run(() =>
                {
                    for (int i = 0; i < MaxCount / threadCount; i++)
                    {
                        doubleBuffer.Add(foo);
                    }
                });
                taskList[j] = task;
            }

            await Task.WhenAll(taskList);
        }

        /// <summary>
        /// 使用链表的双缓存
        /// </summary>
        /// <returns></returns>
        [Benchmark()]
        public async Task AddTaskToDoubleBufferWithLinkedListMultiThread()
        {
            var foo = new Foo();

            var doubleBuffer = new DoubleBuffer<LinkedList<Foo>, Foo>(new LinkedList<Foo>(), new LinkedList<Foo>());

            var threadCount = 10;

            var taskList = new Task[threadCount];

            for (int j = 0; j < threadCount; j++)
            {
                var task = Task.Run(() =>
                {
                    for (int i = 0; i < MaxCount / threadCount; i++)
                    {
                        doubleBuffer.Add(foo);
                    }
                });
                taskList[j] = task;
            }

            await Task.WhenAll(taskList);
        }

        [Benchmark()]
        public void AddTaskToLegacyDoubleBufferWithReaderWriterLockSlim()
        {
            var foo = new Foo();
            var doubleBuffer =
                new DoubleBufferWithReaderWriterLockSlim<List<Foo>, Foo>(new List<Foo>(MaxCount),
                    new List<Foo>(MaxCount));

            for (int i = 0; i < MaxCount; i++)
            {
                doubleBuffer.Add(foo);
            }
        }

        [Benchmark()]
        public async Task AddTaskToLegacyDoubleBufferWithReaderWriterLockSlimMultiThread()
        {
            var foo = new Foo();
            var doubleBuffer =
                new DoubleBufferWithReaderWriterLockSlim<List<Foo>, Foo>(new List<Foo>(MaxCount),
                    new List<Foo>(MaxCount));

            var threadCount = 10;

            var taskList = new Task[threadCount];

            for (int j = 0; j < threadCount; j++)
            {
                var task = Task.Run(() =>
                {
                    for (int i = 0; i < MaxCount / threadCount; i++)
                    {
                        doubleBuffer.Add(foo);
                    }
                });
                taskList[j] = task;
            }

            await Task.WhenAll(taskList);
        }

        private const int MaxCount = 10000;

        class Foo
        {
        }

        /// <summary>
        /// 使用读者写者锁的方法，因为在 Add 的时候，是允许多个线程一起写入的，尽管下面代码没有处理多线程同时写入的坑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU"></typeparam>
        /// 但是性能测试发现使用读者写者锁的性能更差
        /*
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.19041
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.402
  [Host] : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT  [AttachedDebugger]

Job=InProcess  Toolchain=InProcessEmitToolchain

|                                                         Method |        Mean |       Error |      StdDev |  Ratio | RatioSD |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|--------------------------------------------------------------- |------------:|------------:|------------:|-------:|--------:|--------:|--------:|--------:|----------:|
|                                         AddTaskToConcurrentBag |    539.1 us |    10.09 us |     9.91 us |   1.00 |    0.00 | 54.6875 | 30.2734 | 27.3438 | 256.45 KB |
|                          AddTaskToConcurrentBagWithMultiThread |    857.0 us |    16.84 us |    18.72 us |   1.59 |    0.05 | 35.1563 | 17.5781 |  3.9063 | 163.54 KB |
|                            AddTaskToDoubleBufferWithLock |    259.0 us |     2.98 us |     2.64 us |   0.48 |    0.01 | 37.5977 |  9.2773 |       - | 156.45 KB |
|                 AddTaskToDoubleBufferWithLockMultiThread |    599.9 us |     7.60 us |     6.74 us |   1.11 |    0.02 | 38.0859 |  8.7891 |       - | 159.63 KB |
|            AddTaskToDoubleBufferWithReaderWriterLockSlim |    485.8 us |     9.63 us |     9.89 us |   0.90 |    0.02 | 37.5977 |  9.2773 |       - | 156.55 KB |
| AddTaskToDoubleBufferWithReaderWriterLockSlimMultiThread | 62,228.6 us | 2,209.10 us | 6,513.57 us | 118.39 |   13.81 |       - |       - |       - | 160.15 KB |
         */
        class DoubleBufferWithReaderWriterLockSlim<T, TU> where T : class, ICollection<TU>
        {
            /// <summary>
            /// 创建双缓存
            /// </summary>
            /// <param name="aList"></param>
            /// <param name="bList"></param>
            public DoubleBufferWithReaderWriterLockSlim(T aList, T bList)
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
                _readerWriterLockSlim.EnterReadLock();
                try
                {
                    CurrentList.Add(t);
                }
                finally
                {
                    _readerWriterLockSlim.ExitReadLock();
                }
            }

            /// <summary>
            /// 切换缓存
            /// </summary>
            /// <returns></returns>
            public T SwitchBuffer()
            {
                _readerWriterLockSlim.EnterWriteLock();
                try
                {
                    if (ReferenceEquals(CurrentList, AList))
                    {
                        CurrentList = BList;
                        return AList;
                    }
                    else
                    {
                        CurrentList = AList;
                        return BList;
                    }
                }
                finally
                {
                    _readerWriterLockSlim.ExitWriteLock();
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

                    await action(buffer);
                    buffer.Clear();
                }
            }

            private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

            private T CurrentList { set; get; }

            private T AList { get; }
            private T BList { get; }
        }
    }
}
