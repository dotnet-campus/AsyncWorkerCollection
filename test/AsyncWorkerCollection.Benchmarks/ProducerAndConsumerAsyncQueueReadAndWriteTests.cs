using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using dotnetCampus.Threading;

namespace AsyncWorkerCollection.Benchmarks
{
    /// <summary>
    /// 生产者消费者同时进行读写的测试
    /// </summary>
    [BenchmarkCategory(nameof(ProducerAndConsumerAsyncQueueReadAndWriteTests))]
    public class ProducerAndConsumerAsyncQueueReadAndWriteTests
    {
        [Benchmark()]
        public async Task DoubleBufferTaskReadAndWrite()
        {
            var doubleBufferTask = new DoubleBufferTask<Foo>(list => Task.CompletedTask);
            var foo = new Foo();

            for (int i = 0; i < MaxCount; i++)
            {
                doubleBufferTask.AddTask(foo);
            }

            doubleBufferTask.Finish();
            await doubleBufferTask.WaitAllTaskFinish();
        }

        [Benchmark()]
        public async Task DoubleBufferTaskWithCapacityReadAndWrite()
        {
            var doubleBufferTask = new DoubleBufferTask<List<Foo>, Foo>(new List<Foo>(MaxCount),
                new List<Foo>(MaxCount), list => Task.CompletedTask);
            var foo = new Foo();

            for (int i = 0; i < MaxCount; i++)
            {
                doubleBufferTask.AddTask(foo);
            }

            doubleBufferTask.Finish();
            await doubleBufferTask.WaitAllTaskFinish();
        }

        [Benchmark()]
        [Arguments(2)]
        [Arguments(5)]
        [Arguments(10)]
        public async Task DoubleBufferTaskWithMultiThreadReadAndWrite(int threadCount)
        {
            var doubleBufferTask = new DoubleBufferTask<List<Foo>, Foo>(new List<Foo>(MaxCount),
                new List<Foo>(MaxCount), list => Task.CompletedTask);
            var foo = new Foo();

            var taskList = new Task[threadCount];

            for (int j = 0; j < threadCount; j++)
            {
                var task = Task.Run(() =>
                {
                    for (int i = 0; i < MaxCount / threadCount; i++)
                    {
                        doubleBufferTask.AddTask(foo);
                    }
                });
                taskList[j] = task;
            }

            await Task.WhenAll(taskList);

            doubleBufferTask.Finish();
            await doubleBufferTask.WaitAllTaskFinish();
        }

        [Benchmark()]
        public async Task AsyncQueueEnqueueAndDequeueTest()
        {
            var asyncQueue = new AsyncQueue<Foo>();
            var foo = new Foo();

            for (int i = 0; i < MaxCount; i++)
            {
                asyncQueue.Enqueue(foo);
            }

            for (int i = 0; i < MaxCount; i++)
            {
                var temp = await asyncQueue.DequeueAsync();
            }
        }

        [Benchmark()]
        public async Task AsyncQueueEnqueueAndDequeueTestWithMultiThread()
        {
            var asyncQueue = new AsyncQueue<Foo>();
            var foo = new Foo();
            var task = Task.Run(async () =>
            {
                int n = 0;
                while (true)
                {
                    n++;
                    if (n == MaxCount)
                    {
                        break;
                    }

                    var temp = await asyncQueue.DequeueAsync();
                    if (temp is null)
                    {
                        return;
                    }
                }
            });

            for (int i = 0; i < MaxCount; i++)
            {
                asyncQueue.Enqueue(foo);
            }

            await task;
        }

        [Benchmark(Baseline = true)]
        public async Task ChannelReadAndWriteTest()
        {
            var foo = new Foo();
            var bounded = System.Threading.Channels.Channel.CreateBounded<Foo>(MaxCount);

            for (int i = 0; i < MaxCount; i++)
            {
                await bounded.Writer.WriteAsync(foo);
            }

            int n = 0;

            await foreach (var temp in bounded.Reader.ReadAllAsync())
            {
                n++;
                if (n == MaxCount)
                {
                    break;
                }
            }
        }

        [Benchmark()]
        public async Task ChannelReadAndWriteTestWithMultiThread()
        {
            var foo = new Foo();
            var bounded = System.Threading.Channels.Channel.CreateBounded<Foo>(MaxCount);

            var task = Task.Run(async () =>
            {
                int n = 0;

                await foreach (var temp in bounded.Reader.ReadAllAsync())
                {
                    n++;
                    if (n == MaxCount)
                    {
                        break;
                    }
                }
            });

            for (int i = 0; i < MaxCount; i++)
            {
                await bounded.Writer.WriteAsync(foo);
            }

            await task;
        }

        private const int MaxCount = 1000;

        class Foo
        {
        }
    }
}