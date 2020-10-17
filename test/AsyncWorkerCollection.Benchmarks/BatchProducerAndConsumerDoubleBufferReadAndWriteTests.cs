using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using dotnetCampus.Threading;

namespace AsyncWorkerCollection.Benchmarks
{
    /// <summary>
    /// 批量任务的双缓存性能对比
    /// </summary>
    [BenchmarkCategory(nameof(BatchProducerAndConsumerDoubleBufferReadAndWriteTests))]
    public class BatchProducerAndConsumerDoubleBufferReadAndWriteTests
    {
        [Benchmark()]
        public async Task DoubleBufferTaskReadAndWriteTestWithMultiThread()
        {
            const int threadCount = 1;

            var doubleBufferTask = new DoubleBufferTask<List<Foo>, Foo>(new List<Foo>(MaxCount),
                new List<Foo>(MaxCount), async list =>
                {
                    await StartDo();
                });
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

        [Benchmark(Baseline = true)]
        public async Task ChannelReadAndWriteTestWithMultiThread()
        {
            var foo = new Foo();
            var bounded = System.Threading.Channels.Channel.CreateBounded<Foo>(MaxCount);

            var task = Task.Run(async () =>
            {
                int n = 0;

                await foreach (var temp in bounded.Reader.ReadAllAsync())
                {
                    await StartDo();
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

        /// <summary>
        /// 开始执行，如文件写入等，无论是写入多少条，都需要有开始的时间
        /// </summary>
        private async Task StartDo()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        private const int MaxCount = 1000;

        class Foo
        {
        }
    }
}
