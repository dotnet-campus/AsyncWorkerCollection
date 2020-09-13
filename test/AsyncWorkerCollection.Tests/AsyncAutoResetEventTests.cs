using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class AsyncAutoResetEventTests
    {
        [ContractTestCase]
        public void WaitForSuccessOrResult()
        {
            "当使用 Set 次数超过 WaitOneAsync 次数，多余的 Set 只被计算一次".Test(() =>
            {
                Console.WriteLine("当使用 Set 次数超过 WaitOneAsync 次数，多余的 Set 只被计算一次");
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                var task1 = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                for (var i = 0; i < 5; i++)
                {
                    asyncAutoResetEvent.Set();
                    // 先干掉第一次的 task 方法，测试超过了第一次的等待
                    // 如果没有下面方法，可以认为 task1 还没创建出来，此时调用 Set 还是在没有线程等待
                    Task.WaitAny(task1, Task.Delay(TimeSpan.FromSeconds(1)));
                }

                var taskList = new List<Task>();
                for (var i = 0; i < 5; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        Console.WriteLine("进入调用");
                        await asyncAutoResetEvent.WaitOneAsync();
                        mock.Object.Do();
                    });
                    taskList.Add(task);
                }

                foreach (var task in taskList)
                {
                    Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));
                }

                // Assert
                mock.Verify(job => job.Do(), Times.Exactly(2));
            });

            "在先设置 Set 然后再 WaitOneAsync 只有一个线程执行".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                asyncAutoResetEvent.Set();
                var task1 = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                var task2 = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                Task.WaitAny(task1, task2, Task.Delay(TimeSpan.FromSeconds(1)));

                // Assert
                mock.Verify(job => job.Do(), Times.Once);
            });

            "使用 AsyncAutoResetEvent 设置一次 Set 对应一次 WaitOneAsync 的线程执行".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                var taskList = new List<Task>(10);
                for (var i = 0; i < 10; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        await asyncAutoResetEvent.WaitOneAsync();
                        mock.Object.Do();
                    });
                    taskList.Add(task);
                }

                // 等待 Task 都进入 await 方法
                // 如果没有等待，可以都在线程创建上面，此时调用多次的 Set 只是做初始化
                // 也就是当前没有线程等待，然后进行多次 Set 方法
                foreach (var task in taskList)
                {
                    Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));
                }

                for (var i = 0; i < 5; i++)
                {
                    asyncAutoResetEvent.Set();
                }

                foreach (var task in taskList)
                {
                    Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));
                }

                // Assert
                mock.Verify(job => job.Do(), Times.Exactly(5));
            });

            "构造函数设置为 true 等待 WaitOneAsync 的线程会执行".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(true);
                var mock = new Mock<IFakeJob>();

                // Action
                var task = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));

                // Assert
                mock.Verify(job => job.Do(), Times.Once);
            });

            "构造函数设置为 false 等待 WaitOneAsync 的线程不会执行".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                var task = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));

                // Assert
                mock.Verify(job => job.Do(), Times.Never);
            });

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

        public interface IFakeJob
        {
            void Do();
        }
    }
}