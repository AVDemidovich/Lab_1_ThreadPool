using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace СПП_Lab_1_ThreadPool
{
    public class FixedThreadPool: IDisposable
    {
        private int numberThreads;

        private ManualResetEvent stopEvent;
        private bool isStoping;
        private object stopLock;

        private Dictionary<int, ManualResetEvent> threadsEvent;
        private Thread[] threads;
        private List<Task> tasks;

        private ManualResetEvent scheduleEvent;
        private Thread scheduleThread;

        private bool isDisposed;

        public FixedThreadPool() : this(Environment.ProcessorCount) { }

        public FixedThreadPool(int numberThreads)
        {
            if (numberThreads <= 0)
                throw new ArgumentException("numberThreads", "Количество потоков должно быть больше нуля.");

            this.numberThreads = numberThreads;

            this.stopLock = new object();
            this.stopEvent = new ManualResetEvent(false);

            this.scheduleEvent = new ManualResetEvent(false);
            this.scheduleThread = new Thread(SelectAndStartFreeThread) { Name = "Schedule Thread", IsBackground = true };
            scheduleThread.Start();

            this.threads = new Thread[numberThreads];
            this.threadsEvent = new Dictionary<int, ManualResetEvent>(numberThreads);

            for (int i = 0; i < numberThreads; i++)
            {
                threads[i] = new Thread(ThreadWork) { Name = "Pool Thread", IsBackground = true };
                threadsEvent.Add(threads[i].ManagedThreadId, new ManualResetEvent(false));

                threads[i].Start();
            }

            this.tasks = new List<Task>();
        }

        ~FixedThreadPool()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    scheduleThread.Abort();
                    scheduleEvent.Dispose();

                    for (int i = 0; i < numberThreads; i++)
                    {
                        threads[i].Abort();
                        threadsEvent[threads[i].ManagedThreadId].Dispose();
                    }
                }

                isDisposed = true;
            }
        }

        private Task SelectTask()
        {
            lock (tasks)
            {
                if (tasks.Count == 0)
                    throw new ArgumentException();

                var waitingTasks = tasks.Where(t => !t.IsRunned);
                return tasks.First();
            }
        }

        private void ThreadWork()
        {
            while (true)
            {
                threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();

                Task task = SelectTask();
                if (task != null)
                {
                    try
                    {
                        task.Execute();
                    }
                    finally
                    {
                        RemoveTask(task);
                        if (isStoping)
                            stopEvent.Set();
                        threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                    }
                }
            }
        }

        private void SelectAndStartFreeThread()
        {
            while (true)
            {
                scheduleEvent.WaitOne();
                lock (threads)
                {
                    foreach (var thread in threads)
                    {
                        if (threadsEvent[thread.ManagedThreadId].WaitOne(0) == false)
                        {
                            threadsEvent[thread.ManagedThreadId].Set();
                            break;
                        }
                    }
                }

                scheduleEvent.Reset();
            }
        }

        private void AddTask(Task task)
        {
            lock (tasks)
            {
                tasks.Add(task);
            }

            scheduleEvent.Set();
        }

        private void RemoveTask(Task task)
        {
            lock (tasks)
            {
                tasks.Remove(task);
            }

            if (tasks.Count > 0 && tasks.Where(t => !t.IsRunned).Count() > 0)
            {
                scheduleEvent.Set();
            }
        }

        public bool Execute(Task task)
        {
            if (task == null)
                throw new ArgumentNullException("task", "The Task can't be null.");

            lock (stopLock)
            {
                if (isStoping)
                {
                    return false;
                }

                AddTask(task);
                return true;
            }
        }

        public bool ExecuteRange(IEnumerable<Task> tasks)
        {
            bool result = true;
            foreach (var task in tasks)
            {
                if (!Execute(task))
                    result = false;
            }

            return result;
        }

        public void Stop()
        {
            lock (stopLock)
            {
                isStoping = true;
            }

            while (tasks.Count > 0)
            {
                stopEvent.WaitOne();
                stopEvent.Reset();
            }

            Dispose(true);
        }
    }
}
