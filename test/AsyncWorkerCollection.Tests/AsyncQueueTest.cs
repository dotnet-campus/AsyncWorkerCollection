using System;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class AsyncQueueTest
    {
        [ContractTestCase]
        public void DisposeTest()
        {
            "在进行销毁之前，存在元素没有出队，可以成功销毁".Test(() =>
            {
                // Arrange
                var asyncQueue = new AsyncQueue<int>();
                asyncQueue.Enqueue(0);
                asyncQueue.Enqueue(0);

                // Action
                asyncQueue.Dispose();

                // Assert
                Assert.AreEqual(0, asyncQueue.Count);
            });
        }
    }
}
