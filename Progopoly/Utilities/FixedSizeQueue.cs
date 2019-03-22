using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Progopoly.Utilities
{
    public class FixedSizedQueue<T>
    {
        ConcurrentQueue<T> q = new ConcurrentQueue<T>();
        private object lockObject = new object();
        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public int Count()
        {
            return q.Count();
        }

        public void Enqueue(T obj)
        {
            q.Enqueue(obj);
            lock (lockObject)
            {
                T overflow;
                while (q.Count > Size && q.TryDequeue(out overflow)) ;
            }
        }
    }
}