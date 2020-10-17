using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnetCampus.Threading.Reentrancy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests.Reentrancy
{
    [TestClass]
    public class QueueReentrancyTaskTests
    {
        [ContractTestCase]
        public void InvokeAsync()
        {
            "并发执行大量任务，最终返回值按顺序获取到。".Test(async () =>
            {
                // Arrange
                var concurrentCount = 100;
                var resultList = new List<int>();
                var reentrancy = new QueueReentrancyTask<int, int>(async i =>
                {
                    await Task.Delay(10).ConfigureAwait(false);
                    resultList.Add(i);
                    return i;
                });

                // Action
                await Task.WhenAll(Enumerable.Range(0, concurrentCount).Select(i => reentrancy.InvokeAsync(i)));

                // Assert
                for (var i = 0; i < concurrentCount; i++)
                {
                    Assert.AreEqual(i, resultList[i]);
                }
            });
        }
    }
}
