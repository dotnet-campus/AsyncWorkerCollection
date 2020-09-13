using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class AsyncAutoResetEventTests
    {
        [ContractTestCase]
        public void Set()
        {
            "在 WaitOne 之前调用多次 Set 只有在调用之后让一个 WaitOne 方法继续".Test(() =>
            {
                using var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                for (int i = 0; i < 1000; i++)
                {
                    asyncAutoResetEvent.Set();
                }

                var count = 0;
                var taskList = new List<Task>();
                for (int i = 0; i < 10; i++)
                {
                    taskList.Add(Task.Run(async () =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        await asyncAutoResetEvent.WaitOneAsync();
                        Interlocked.Increment(ref count);
                    }));
                }

                // 只有一个执行
                // 单元测试有一个坑，也就是在不同的设备上，也许有设备就是不分配线程，所以这个单元测试也许会在执行的时候，发现没有一个线程执行完成
                taskList.Add(Task.Delay(TimeSpan.FromSeconds(5)));
                // 在上面加入一个等待 5 秒的线程，此时理论上有一个线程执行完成
                Task.WaitAny(taskList.ToArray());
                // 什么时候是 0 的值？在没有分配线程，也就是没有一个 Task.Run 进入
                Assert.AreEqual(true, count <= 1);
                // 一定有超过 9 个线程没有执行完成
                Assert.AreEqual(true, taskList.Count(task => !task.IsCompleted) >= 9);
            });
        }

        [ContractTestCase]
        public void ReleaseObject()
        {
            "在调用释放之后，所有的等待将会被释放，同时释放的值是 false 值".Test(() =>
            {
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var manualResetEvent = new ManualResetEvent(false);
                var task = Task.Run(async () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var t = asyncAutoResetEvent.WaitOneAsync();
                    manualResetEvent.Set();

                    return await t;
                });
                // 解决单元测试里面 Task.Run 启动太慢
                manualResetEvent.WaitOne();
                asyncAutoResetEvent.Dispose();

                task.Wait();
                var taskResult = task.Result;
                Assert.AreEqual(false, taskResult);
            });
        }
    }
}