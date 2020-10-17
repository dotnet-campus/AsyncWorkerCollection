using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using dotnetCampus.Threading;

namespace AsyncWorkerCollection.Benchmarks
{
    /// <summary>
    /// 生产者消费者仅写入的测试
    /// </summary>
    [BenchmarkCategory(nameof(ProducerAndConsumerAsyncQueueWriteTests))]
    public class ProducerAndConsumerAsyncQueueWriteTests
    {
        [Benchmark()]
        public void AsyncQueueEnqueueTest()
        {
            var asyncQueue = new AsyncQueue<Foo>();

            var foo = new Foo();
            for (int i = 0; i < MaxCount; i++)
            {
                asyncQueue.Enqueue(foo);
            }
        }

        [Benchmark(Baseline = true)]
        public async Task ChannelWriteAsyncTest()
        {
            var foo = new Foo();
            var bounded = System.Threading.Channels.Channel.CreateBounded<Foo>(MaxCount);

            for (int i = 0; i < MaxCount; i++)
            {
                await bounded.Writer.WriteAsync(foo);
            }
        }

        private const int MaxCount = 1000;

        class Foo
        {
        }
    }
}
