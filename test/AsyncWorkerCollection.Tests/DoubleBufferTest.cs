using System;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class DoubleBufferTest
    {
        [ContractTestCase]
        public void DoAll()
        {
            "多线程随机延迟一边加入元素一边执行，可以执行所有元素".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                var random = new Random();
                const int n = 100;

                var doubleBuffer = new DoubleBuffer<IFoo>();

                var t1 = Task.Run(async () =>
                {
                    for (int i = 0; i < n; i++)
                    {
                        doubleBuffer.Add(mock.Object);
                        await Task.Delay(random.Next(100));
                    }
                });

                var t2 = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    await doubleBuffer.DoAllAsync(async list =>
                    {
                        foreach (var foo in list)
                        {
                            await Task.Delay(random.Next(50));
                            foo.Foo();
                        }
                    });
                });

                Task.WaitAll(t1, t2);

                doubleBuffer.DoAllAsync(async list =>
                {
                    foreach (var foo in list)
                    {
                        await Task.Delay(random.Next(50));
                        foo.Foo();
                    }
                }).Wait();

                mock.Verify(foo => foo.Foo(), Times.Exactly(n));
            });

            "多线程一边加入元素一边执行，可以执行所有元素".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                const int n = 10000;

                var doubleBuffer = new DoubleBuffer<IFoo>();

                for (int i = 0; i < n; i++)
                {
                    doubleBuffer.Add(mock.Object);
                }

                var t1 = Task.Run(() =>
                {
                    for (int i = 0; i < n; i++)
                    {
                        doubleBuffer.Add(mock.Object);
                    }
                });

                var t2 = Task.Run(() => { doubleBuffer.DoAll(list => list.ForEach(foo => foo.Foo())); });

                Task.WaitAll(t1, t2);

                // 没有执行一次
                mock.Verify(foo => foo.Foo(), Times.Exactly(n * 2));
            });

            "给定10次元素，执行 DoAll 元素执行10次".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                const int n = 10;

                var doubleBuffer = new DoubleBuffer<IFoo>();

                for (int i = 0; i < n; i++)
                {
                    doubleBuffer.Add(mock.Object);
                }

                doubleBuffer.DoAll(list => list.ForEach(foo => foo.Foo()));

                // 没有执行一次
                mock.Verify(foo => foo.Foo(), Times.Exactly(n));
            });

            "没有给定缓存内容，执行 DoAll 啥都不做".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                var doubleBuffer = new DoubleBuffer<IFoo>();
                doubleBuffer.DoAll(list => list.ForEach(foo => foo.Foo()));

                // 没有执行一次
                mock.Verify(foo => foo.Foo(), Times.Never);
            });
        }

        public interface IFoo
        {
            void Foo();
        }
    }
}
