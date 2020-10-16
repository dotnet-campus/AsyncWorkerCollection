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
            "µ±Ê¹ÓÃ Set ´ÎÊý³¬¹ý WaitOneAsync ´ÎÊý£¬¶àÓàµÄ Set Ö»±»¼ÆËãÒ»´Î".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                // ÏÈ¼ÓÈëÒ»¸öµÈ´ýµÄÏß³Ì£¬ÓÃÓÚµÈ´ýµÚÒ»´ÎµÄ Set ¶ÔÓ¦µÄµÈ´ý
                var manualResetEvent = new ManualResetEvent(false);
                var task1 = Task.Run(async () =>
                {
                    var task = asyncAutoResetEvent.WaitOneAsync();
                    manualResetEvent.Set();
                    await task;
                    mock.Object.Do();
                });
                // Ê¹ÓÃ manualResetEvent ¿ÉÒÔµÈ´ýÈÃ task1 Ö´ÐÐµ½ÁË WaitOne ·½·¨
                manualResetEvent.WaitOne();

                for (var i = 0; i < 5; i++)
                {
                    asyncAutoResetEvent.Set();
                }

                var taskList = new List<Task>();
                for (var i = 0; i < 5; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        Console.WriteLine("½øÈëµ÷ÓÃ");
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

            "ÔÚÏÈÉèÖÃ Set È»ºóÔÙ WaitOneAsync Ö»ÓÐÒ»¸öÏß³ÌÖ´ÐÐ".Test(() =>
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

            "Ê¹ÓÃ AsyncAutoResetEvent ÉèÖÃÒ»´Î Set ¶ÔÓ¦Ò»´Î WaitOneAsync µÄÏß³ÌÖ´ÐÐ".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                var taskList = new List<Task>(10);
                // Ê¹ÓÃ SemaphoreSlim ÈÃ²âÊÔÏß³ÌÈ«²¿´´½¨
                var semaphoreSlim = new SemaphoreSlim(0, 10);
                for (var i = 0; i < 10; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        var t = asyncAutoResetEvent.WaitOneAsync();
                        semaphoreSlim.Release();
                        await t;
                        mock.Object.Do();
                    });
                    taskList.Add(task);
                }

                // µÈ´ý Task ¶¼½øÈë await ·½·¨
                // Èç¹ûÃ»ÓÐµÈ´ý£¬¿ÉÒÔ¶¼ÔÚÏß³Ì´´½¨ÉÏÃæ£¬´ËÊ±µ÷ÓÃ¶à´ÎµÄ Set Ö»ÊÇ×ö³õÊ¼»¯
                // Ò²¾ÍÊÇµ±Ç°Ã»ÓÐÏß³ÌµÈ´ý£¬È»ºó½øÐÐ¶à´Î Set ·½·¨
                for (int i = 0; i < 10; i++)
                {
                    semaphoreSlim.Wait();
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

            "¹¹Ôìº¯ÊýÉèÖÃÎª true µÈ´ý WaitOneAsync µÄÏß³Ì»áÖ´ÐÐ".Test(() =>
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

            "¹¹Ôìº¯ÊýÉèÖÃÎª false µÈ´ý WaitOneAsync µÄÏß³Ì²»»áÖ´ÐÐ".Test(() =>
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

            "ÔÚ WaitOne Ö®Ç°µ÷ÓÃ¶à´Î Set Ö»ÓÐÔÚµ÷ÓÃÖ®ºóÈÃÒ»¸ö WaitOne ·½·¨¼ÌÐø".Test(() =>
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

                // Ö»ÓÐÒ»¸öÖ´ÐÐ
                // µ¥Ôª²âÊÔÓÐÒ»¸ö¿Ó£¬Ò²¾ÍÊÇÔÚ²»Í¬µÄÉè±¸ÉÏ£¬Ò²ÐíÓÐÉè±¸¾ÍÊÇ²»·ÖÅäÏß³Ì£¬ËùÒÔÕâ¸öµ¥Ôª²âÊÔÒ²Ðí»áÔÚÖ´ÐÐµÄÊ±ºò£¬·¢ÏÖÃ»ÓÐÒ»¸öÏß³ÌÖ´ÐÐÍê³É
                taskList.Add(Task.Delay(TimeSpan.FromSeconds(5)));
                // ÔÚÉÏÃæ¼ÓÈëÒ»¸öµÈ´ý 5 ÃëµÄÏß³Ì£¬´ËÊ±ÀíÂÛÉÏÓÐÒ»¸öÏß³ÌÖ´ÐÐÍê³É
                Task.WaitAny(taskList.ToArray());
                // Ê²Ã´Ê±ºòÊÇ 0 µÄÖµ£¿ÔÚÃ»ÓÐ·ÖÅäÏß³Ì£¬Ò²¾ÍÊÇÃ»ÓÐÒ»¸ö Task.Run ½øÈë
                Assert.AreEqual(true, count <= 1);
                // Ò»¶¨ÓÐ³¬¹ý 9 ¸öÏß³ÌÃ»ÓÐÖ´ÐÐÍê³É
                Assert.AreEqual(true, taskList.Count(task => !task.IsCompleted) >= 9);
            });
        }

        [ContractTestCase]
        public void ReleaseObject()
        {
            "ÔÚµ÷ÓÃÊÍ·ÅÖ®ºó£¬ËùÓÐµÄµÈ´ý½«»á±»ÊÍ·Å£¬Í¬Ê±ÊÍ·ÅµÄÖµÊÇ false Öµ".Test(() =>
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
                // ½â¾öµ¥Ôª²âÊÔÀïÃæ Task.Run Æô¶¯Ì«Âý
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
