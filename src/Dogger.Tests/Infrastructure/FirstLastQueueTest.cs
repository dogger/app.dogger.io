using Dogger.Infrastructure;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Infrastructure
{
    [TestClass]
    public class FirstLastQueueTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void Enqueue_ZeroElementsInQueue_AddsSingleElement()
        {
            //Arrange
            var queue = new FirstLastQueue<string>();
            Assert.AreEqual(0, queue.Count);

            //Act
            queue.Enqueue("item-1");

            //Assert
            Assert.AreEqual(1, queue.Count);

            var value = queue.Dequeue();
            Assert.AreEqual("item-1", value);

            Assert.AreEqual(0, queue.Count);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void Enqueue_OneElementInQueue_AddsSecondElement()
        {
            //Arrange
            var queue = new FirstLastQueue<string>();
            queue.Enqueue("item-1");
            Assert.AreEqual(1, queue.Count);

            //Act
            queue.Enqueue("item-2");

            //Assert
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual("item-1", queue.Dequeue());
            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual("item-2", queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void Enqueue_TwoElementsInQueue_AddsThirdElementAndRemovesSecondElement()
        {
            //Arrange
            var queue = new FirstLastQueue<string>();
            queue.Enqueue("item-1");
            queue.Enqueue("item-2");
            Assert.AreEqual(2, queue.Count);

            //Act
            queue.Enqueue("item-3");

            //Assert
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual("item-1", queue.Dequeue());
            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual("item-3", queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void Dequeue_ZeroElementsInQueue_DoesNothingAndReturnsNull()
        {
            //Arrange
            var queue = new FirstLastQueue<string>();
            Assert.AreEqual(0, queue.Count);

            //Act
            var value = queue.Dequeue();

            //Assert
            Assert.AreEqual(0, queue.Count);
            Assert.IsNull(value);
        }
    }
}
