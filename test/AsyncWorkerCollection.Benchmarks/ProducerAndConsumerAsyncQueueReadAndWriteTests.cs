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
        public async Task AsyncQueueEnqueueAndDequeueTest()
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

        private const int MaxCount = 1000;

        class Foo
        {
        }
    }
}