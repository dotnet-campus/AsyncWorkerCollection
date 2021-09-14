using System;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class AsyncTaskQueueTest
    {
        [ContractTestCase]
        public void DisposeTest()
        {
            "连续开始任务的时候，无法立刻释放".Test(async () =>
            {
                AsyncTaskQueue asyncTaskQueue = new AsyncTaskQueue() { AutoCancelPreviousTask = true, UseSingleThread = true };

                var autoResetEvent = new AutoResetEvent(false);
                _ = Task.Run(() =>
                {
                    autoResetEvent.WaitOne();
                    asyncTaskQueue.Dispose();
                });

                var result = await asyncTaskQueue.ExecuteAsync(async () =>
                {
                    await Task.Delay(10);
                    autoResetEvent.Set();
                    return 1;
                });

                await Task.Delay(100);

                for (int i = 0; i < 10; i++)
                {
                   _ = asyncTaskQueue.ExecuteAsync<bool>((Func<Task>)(() => Task.CompletedTask));
                }

                await Task.Delay(1000);
                autoResetEvent.Set();

                // 没有抛出异常就是符合预期
                Assert.AreEqual(true, result.IsInvalid);
                Assert.AreEqual(1, result.Result);
            });

            "在执行任务结束的时候调用 AsyncTaskQueue 销毁方法，可以不抛异常销毁".Test(async () =>
            {
                AsyncTaskQueue asyncTaskQueue = new AsyncTaskQueue() { AutoCancelPreviousTask = true, UseSingleThread = true };
                var autoResetEvent = new AutoResetEvent(false);
                _ = Task.Run(() =>
                {
                    autoResetEvent.WaitOne();
                    asyncTaskQueue.Dispose();
                });

                var result = await asyncTaskQueue.ExecuteAsync(async () =>
                {
                    await Task.Delay(10);
                    autoResetEvent.Set();
                    return 1;
                });

                Thread.Sleep(2000);

                Assert.AreEqual(true, result.IsInvalid);
                Assert.AreEqual(1, result.Result);
            });

            "调用 AsyncTaskQueue 销毁方法，可以不抛异常销毁".Test(async () =>
            {
                AsyncTaskQueue asyncTaskQueue = new AsyncTaskQueue() { AutoCancelPreviousTask = true, UseSingleThread = true };

                var result = await asyncTaskQueue.ExecuteAsync(async () =>
                {
                    await Task.Delay(10);
                    return 0;
                });

                asyncTaskQueue.Dispose();

                Thread.Sleep(2000);

                Assert.AreEqual(true, result.IsInvalid);
                Assert.AreEqual(0, result.Result);
            });
        }
    }
}
