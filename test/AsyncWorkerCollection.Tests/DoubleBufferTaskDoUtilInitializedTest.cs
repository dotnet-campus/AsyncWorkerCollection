using System.Collections.Generic;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class DoubleBufferTaskDoUtilInitializedTest
    {
        [ContractTestCase]
        public void DoUtilInitialized()
        {
            "ÔÚµ÷ÓÃ³õÊ¼»¯Ö®ºó£¬²Å¿ªÊ¼Ö´ÐÐÈÎÎñ".Test(async () =>
            {
                var mock = new Mock<IWorker>();
                mock.Setup(worker => worker.DoTask(It.IsAny<List<int>>()));

                var doubleBufferTaskDoUtilInitialized = new DoubleBufferLazyInitializeTask<int>(mock.Object.DoTask);
                for (int i = 0; i < 100; i++)
                {
                    doubleBufferTaskDoUtilInitialized.AddTask(i);
                }

                var taskList = new List<Task>();

                for (int i = 0; i < 100; i++)
                {
                    taskList.Add(Task.Run(() => doubleBufferTaskDoUtilInitialized.AddTask(0)));
                }

                await Task.WhenAll(taskList);
                doubleBufferTaskDoUtilInitialized.Finish();

                var waitAllTaskFinish = doubleBufferTaskDoUtilInitialized.WaitAllTaskFinish();

                mock.Verify(worker => worker.DoTask(It.IsAny<List<int>>()), Times.Never);
                Assert.AreEqual(false, waitAllTaskFinish.IsCompleted);

                // µ÷ÓÃ³õÊ¼»¯Íê³É
                doubleBufferTaskDoUtilInitialized.OnInitialized();
                await waitAllTaskFinish;
                mock.Verify(worker => worker.DoTask(It.IsAny<List<int>>()), Times.AtLeast(1));
            });

            "ÔÚµ÷ÓÃ³õÊ¼»¯Ö®Ç°£¬²»»áÖ´ÐÐÈÎºÎµÄÈÎÎñ".Test(async () =>
            {
                var mock = new Mock<IWorker>();
                mock.Setup(worker => worker.DoTask(It.IsAny<List<int>>()));

                var doubleBufferTaskDoUtilInitialized = new DoubleBufferLazyInitializeTask<int>(mock.Object.DoTask);
                for (int i = 0; i < 100; i++)
                {
                    doubleBufferTaskDoUtilInitialized.AddTask(i);
                }

                var taskList = new List<Task>();

                for (int i = 0; i < 100; i++)
                {
                    taskList.Add(Task.Run(() => doubleBufferTaskDoUtilInitialized.AddTask(0)));
                }

                await Task.WhenAll(taskList);
                doubleBufferTaskDoUtilInitialized.Finish();

                var waitAllTaskFinish = doubleBufferTaskDoUtilInitialized.WaitAllTaskFinish();

                mock.Verify(worker => worker.DoTask(It.IsAny<List<int>>()), Times.Never);
                Assert.AreEqual(false, waitAllTaskFinish.IsCompleted);
            });
        }

        public interface IWorker
        {
            Task DoTask(List<int> list);
        }
    }
}
