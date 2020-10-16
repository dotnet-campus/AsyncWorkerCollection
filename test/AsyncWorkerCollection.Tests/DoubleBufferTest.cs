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
            "¶àÏß³ÌËæ»úÑÓ³ÙÒ»±ß¼ÓÈëÔªËØÒ»±ßÖ´ÐÐ£¬¿ÉÒÔÖ´ÐÐËùÓÐÔªËØ".Test(() =>
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

            "¶àÏß³ÌÒ»±ß¼ÓÈëÔªËØÒ»±ßÖ´ÐÐ£¬¿ÉÒÔÖ´ÐÐËùÓÐÔªËØ".Test(() =>
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

                // Ã»ÓÐÖ´ÐÐÒ»´Î
                mock.Verify(foo => foo.Foo(), Times.Exactly(n * 2));
            });

            "¸ø¶¨10´ÎÔªËØ£¬Ö´ÐÐ DoAll ÔªËØÖ´ÐÐ10´Î".Test(() =>
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

                // Ã»ÓÐÖ´ÐÐÒ»´Î
                mock.Verify(foo => foo.Foo(), Times.Exactly(n));
            });

            "Ã»ÓÐ¸ø¶¨»º´æÄÚÈÝ£¬Ö´ÐÐ DoAll É¶¶¼²»×ö".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                var doubleBuffer = new DoubleBuffer<IFoo>();
                doubleBuffer.DoAll(list => list.ForEach(foo => foo.Foo()));

                // Ã»ÓÐÖ´ÐÐÒ»´Î
                mock.Verify(foo => foo.Foo(), Times.Never);
            });
        }

        public interface IFoo
        {
            void Foo();
        }
    }
}
