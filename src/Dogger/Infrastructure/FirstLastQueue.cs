using System.Collections.Generic;

namespace Dogger.Infrastructure
{
    /// <summary>
    /// Represents a queue that has 2, and only keeps the last and the first element.
    /// </summary>
    public class FirstLastQueue<T> where T : class
    {
        private readonly LinkedList<T> list;

        public FirstLastQueue()
        {
            this.list = new LinkedList<T>();
        }

        public void Enqueue(T item)
        {
            while (this.list.Count > 1)
                this.list.Remove(this.list.First!.Next!);

            this.list.AddLast(item);
        }

        public T? Peek()
        {
            return this.list.First?.Value;
        }

        public T? Dequeue()
        {
            if (this.list.Count == 0)
                return null;

            var result = Peek();
            this.list.RemoveFirst();

            return result;
        }
    }
}
